using System;
using System.IO;
//using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
//using System.Drawing;
//using System.Drawing.Imaging;

public class Extractor
{
		private static uint UI_BMP_MAGIC_BASE = 3793551360u; //0xE21D0000
		private static uint SPI_FILE_MAGIC_BASE = 318570496u; //0x12FD0000
		private byte[] fileDat = new byte[0];

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct spi_file_head_t
		{
			public uint magic_num;
			public uint file_size;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct ui_head_t
		{
			public uint ui_magic;
			public int offset;
			public ushort x_pos;
			public ushort y_pos;
			public ushort x_width;
			public ushort y_height;
			public uint reserved;
		}
		
		public object BytesToStruct(byte[] buf, int len, Type type)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(len);
			Marshal.Copy(buf, 0, intPtr, len);
			object result = Marshal.PtrToStructure(intPtr, type);
			Marshal.FreeHGlobal(intPtr);
			return result;
		}

		public object BytesToStruct(byte[] buf, Type type)
		{
			return BytesToStruct(buf, buf.Length, type);
		}

		public int LoadUI(string InFile)
		{
			int ImageNum = 0;
			StreamReader streamReader;
			try
			{
				streamReader = new StreamReader(InFile);
			}
			catch (Exception)
			{
				System.Console.WriteLine("Unable to open UI file:" + InFile);
				return ImageNum;
			}
			FileInfo fileInfo = new FileInfo(InFile);
			fileDat = new byte[fileInfo.Length];
			streamReader.BaseStream.Read(fileDat, 0, fileDat.Length);
			streamReader.Close();
			spi_file_head_t spi_file_head_t = default(spi_file_head_t);
			byte[] array = new byte[Marshal.SizeOf((object)spi_file_head_t)];
			Buffer.BlockCopy(fileDat, 0, array, 0, Marshal.SizeOf((object)spi_file_head_t));
			spi_file_head_t = (spi_file_head_t)BytesToStruct(array, Marshal.SizeOf((object)spi_file_head_t), spi_file_head_t.GetType());
			if (spi_file_head_t.magic_num == (SPI_FILE_MAGIC_BASE | 0xC))
			{
				if (spi_file_head_t.file_size + 8 < fileInfo.Length)
				{
					System.Console.WriteLine("The file contains multiple UIs. The file will be expanded into multiple UI files.");
//					string directoryName = Path.GetDirectoryName(InFile);
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(InFile);
					string extension = Path.GetExtension(InFile);
					for (int i = 0; i < fileInfo.Length; i += (int)(spi_file_head_t.file_size + 8))
					{
						Buffer.BlockCopy(fileDat, i, array, 0, Marshal.SizeOf((object)spi_file_head_t));
						spi_file_head_t = (spi_file_head_t)BytesToStruct(array, Marshal.SizeOf((object)spi_file_head_t), spi_file_head_t.GetType());
						if (spi_file_head_t.magic_num != (SPI_FILE_MAGIC_BASE | 0xC))
						{
							System.Console.WriteLine("UI error!");
							break;
						}
						ui_head_t ui_head_t = default(ui_head_t);
						byte[] array2 = new byte[Marshal.SizeOf((object)ui_head_t)];
						Buffer.BlockCopy(fileDat, i + 8 + Marshal.SizeOf((object)ui_head_t), array2, 0, Marshal.SizeOf((object)ui_head_t));
						ui_head_t = (ui_head_t)BytesToStruct(array2, Marshal.SizeOf((object)ui_head_t), ui_head_t.GetType());
						if (ui_head_t.ui_magic == (1 | UI_BMP_MAGIC_BASE))
						{
//							string text = directoryName + "\\" + fileNameWithoutExtension + "_" + ui_head_t.x_width + "x" + ui_head_t.y_height + extension;
							string text = fileNameWithoutExtension + "_" + ui_head_t.x_width + "x" + ui_head_t.y_height + extension;
							StreamWriter streamWriter;
							try
							{
								streamWriter = new StreamWriter(text);
							}
							catch (Exception)
							{
								System.Console.WriteLine("Unable to generate file:" + text);
								return ImageNum;
							}
							streamWriter.BaseStream.Write(fileDat, i, (int)spi_file_head_t.file_size);
							streamWriter.Close();
							System.Console.WriteLine("Extracted UI:" + text);
						}
						ImageNum++;
					}
				}
				else
				{
					System.Console.WriteLine("The file doesn't contain multiple UIs!");
				}
			}
			else
			{
				System.Console.WriteLine("Invalid UI file!");
				return ImageNum;
			}
		return ImageNum;
		}
}

class MainClass
{
    static int Main(string[] args)
    {
        // Test if input arguments were supplied:
        if (args.Length != 1)
        {
            System.Console.WriteLine("CBD multi-resolution UI file extractor v0.1");
            System.Console.WriteLine("Please enter binary UI package.");
            System.Console.WriteLine("Usage: UIextractor <input UI binary>");
            return 1;
        }

        // Get the images.
        Extractor extractor = new Extractor();
        int result = extractor.LoadUI(args[0]);
        System.Console.WriteLine("Extracted {0} UI files from multi-UI file.", result);

        return 0;
    }
}

