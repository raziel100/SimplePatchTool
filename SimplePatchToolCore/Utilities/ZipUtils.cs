﻿using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Compression = SevenZip.Compression;

namespace SimplePatchToolCore
{
	public enum CompressionFormat { LZMA = 0, GZIP = 1 };

	public static class ZipUtils
	{
		// Compress a file into LZMA format
		// Credit: http://stackoverflow.com/questions/7646328/how-to-use-the-7z-sdk-to-compress-and-decompress-a-file
		public static void CompressFileLZMA( string inFile, string outFile )
		{
			Compression.LZMA.Encoder coder = new Compression.LZMA.Encoder();
			using( FileStream input = new FileStream( inFile, FileMode.Open, FileAccess.Read ) )
			using( FileStream output = new FileStream( outFile, FileMode.Create ) )
			{
				// Write the encoder properties
				coder.WriteCoderProperties( output );

				// Write the decompressed file size.
				output.Write( BitConverter.GetBytes( input.Length ), 0, 8 );

				// Encode the file.
				coder.Code( input, output, input.Length, -1, null );
			}
		}

		// Decompress a file from LZMA format
		// Credit: http://stackoverflow.com/questions/7646328/how-to-use-the-7z-sdk-to-compress-and-decompress-a-file
		public static void DecompressFileLZMA( string inFile, string outFile )
		{
			Compression.LZMA.Decoder coder = new Compression.LZMA.Decoder();
			using( FileStream input = new FileStream( inFile, FileMode.Open, FileAccess.Read ) )
			using( FileStream output = new FileStream( outFile, FileMode.Create ) )
			{
				// Read the decoder properties
				byte[] properties = new byte[5];
				input.Read( properties, 0, 5 );

				// Read in the decompress file size.
				byte[] fileLengthBytes = new byte[8];
				input.Read( fileLengthBytes, 0, 8 );
				long fileLength = BitConverter.ToInt64( fileLengthBytes, 0 );

				coder.SetDecoderProperties( properties );
				coder.Code( input, output, input.Length, fileLength, null );
			}
		}

		public static void CompressFolderLZMA( string inFolder, string outFile )
		{
			CompressFolderLZMA( inFolder, outFile, new List<Regex>( 0 ) );
		}

		internal static void CompressFolderLZMA( string inFolder, string outFile, List<Regex> ignoredPathsRegex )
		{
			string tarFilePath = outFile + "tmptar";

			// Source: https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#-create-a-tgz-targz
			using( FileStream outputStream = File.Create( tarFilePath ) )
			using( TarArchive tarArchive = TarArchive.CreateOutputTarArchive( outputStream ) )
			{
				// Currently, SharpZipLib only supports '/', and the folder path must not end with it
				if( inFolder[inFolder.Length - 1] == '\\' || inFolder[inFolder.Length - 1] == '/' )
					inFolder = inFolder.Substring( 0, inFolder.Length - 1 ).Replace( '\\', '/' );
				else
					inFolder = inFolder.Replace( '\\', '/' );

				tarArchive.RootPath = inFolder;
				CreateTarRecursive( tarArchive, new DirectoryInfo( inFolder ), "", ignoredPathsRegex );
			}

			CompressFileLZMA( tarFilePath, outFile );
			File.Delete( tarFilePath );
		}

		public static void DecompressFolderLZMA( string inFile, string outFolder )
		{
			string tarFilePath = outFolder + "tmptar.tar";
			DecompressFileLZMA( inFile, tarFilePath );

			// Source: https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#--simple-full-extract-from-a-tar-archive
			using( Stream inStream = File.OpenRead( tarFilePath ) )
			using( TarArchive tarArchive = TarArchive.CreateInputTarArchive( inStream ) )
			{
				tarArchive.ExtractContents( outFolder );
			}

			File.Delete( tarFilePath );
		}

		// Source: https://github.com/icsharpcode/SharpZipLib/wiki/GZip-and-Tar-Samples#-create-a-tgz-targz
		private static void CreateTarRecursive( TarArchive tarArchive, DirectoryInfo directory, string relativePath, List<Regex> ignoredPathsRegex )
		{
			TarEntry tarEntry = TarEntry.CreateEntryFromFile( directory.FullName );
			tarArchive.WriteEntry( tarEntry, false );

			FileInfo[] files = directory.GetFiles();
			for( int i = 0; i < files.Length; i++ )
			{
				string fileRelativePath = relativePath + files[i].Name;
				if( !ignoredPathsRegex.PathMatchesPattern( fileRelativePath ) )
				{
					tarEntry = TarEntry.CreateEntryFromFile( files[i].FullName );
					tarArchive.WriteEntry( tarEntry, true );
				}
			}

			DirectoryInfo[] subDirectories = directory.GetDirectories();
			for( int i = 0; i < subDirectories.Length; i++ )
			{
				string directoryRelativePath = relativePath + subDirectories[i].Name + Path.DirectorySeparatorChar;
				if( !ignoredPathsRegex.PathMatchesPattern( directoryRelativePath ) )
					CreateTarRecursive( tarArchive, subDirectories[i], directoryRelativePath, ignoredPathsRegex );
			}
		}
	}
}