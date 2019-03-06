using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

public class Extractor
{
		private static int MaxImages = 357;
		private static int B_BITS = 5;
		private static int G_BITS = 6;
		private static int R_BITS = 5;
		private static int R_MASK = (1 << R_BITS) - 1;
		private static int G_MASK = (1 << G_BITS) - 1;
		private static int B_MASK = (1 << B_BITS) - 1;
		private static uint UI_BMP_MAGIC_BASE = 3793551360u; //0xE21D0000
		private static uint SPI_FILE_MAGIC_BASE = 318570496u; //0x12FD0000
//		private static uint SPI_FILE_MAGIC_MASK = 4294901760u; //0xFFFF0000
		private static int SPECIAL_BIT = 1 << B_BITS;
		private static int SPECIAL_BIT_MASK = ~SPECIAL_BIT;
		private byte[] fileDat = new byte[0];
		private Stream[] m_ImageStream = new Stream[MaxImages]; //ToDo: change to only one

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

		private bool GetDataPicture(int w, int h, byte[] data, string fileName)
		{
			Bitmap bitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb);
			int num = 0;
			ushort num2 = 0;
			ushort num3 = 0;
			int num4 = 1;
			for (int i = 0; i < data.Length; i += 2)
			{
				num2 = (ushort)(data[i + 1] + (data[i] << 8));
				if (num4 == -1)
				{
					if ((num2 & 0xF000) != 12288) //0xC000
					{
						System.Console.WriteLine("Wrong format, unable to generate image:" + fileName);
						return false;
					}
					num4 = (num2 & 0xFFF) + 1;
				}
				else if ((num2 & SPECIAL_BIT) != 0)
				{
					num3 = (ushort)(num2 & SPECIAL_BIT_MASK);
					num4 = -1;
				}
				else
				{
					num4 = 1;
					num3 = num2;
				}
				if (num4 < 1)
				{
					continue;
				}
				for (int j = 0; j < num4; j++)
				{
					Color color = Color.FromArgb(((num3 >> B_BITS + G_BITS) & B_MASK) << 3, ((num3 >> B_BITS) & G_MASK) << 2, (num3 & B_MASK) << 3);
					bitmap.SetPixel(num % w, num / w, color);
					num++;
					if (num >= w * h)
					{
						break;
					}
				}
				if (num >= w * h)
				{
					break;
				}
			}
			try
			{
				bitmap.Save(fileName, ImageFormat.Bmp);
				bitmap.Dispose();
				return true;
			}
			catch (Exception ex)
			{
				System.Console.WriteLine(ex.ToString());
			}
			return false;
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
				ui_head_t ui_head_t = default(ui_head_t);
				for (int i = 0; i < MaxImages; i++)
				{
					byte[] array5 = new byte[Marshal.SizeOf((object)ui_head_t)];
					Buffer.BlockCopy(fileDat, 8 + i * Marshal.SizeOf((object)ui_head_t), array5, 0, Marshal.SizeOf((object)ui_head_t));
					ui_head_t = (ui_head_t)BytesToStruct(array5, Marshal.SizeOf((object)ui_head_t), ui_head_t.GetType());
					if (ui_head_t.ui_magic == 0) continue; //Missing image. Skip.
					if (ui_head_t.ui_magic == (uint)(i | (int)UI_BMP_MAGIC_BASE))
					{
						if (ui_head_t.offset < fileDat.Length)
						{
							Buffer.BlockCopy(fileDat, ui_head_t.offset, array, 0, Marshal.SizeOf((object)spi_file_head_t));
							spi_file_head_t = (spi_file_head_t)BytesToStruct(array, Marshal.SizeOf((object)spi_file_head_t), spi_file_head_t.GetType());
							if (spi_file_head_t.magic_num != (uint)((int)UI_BMP_MAGIC_BASE | i))
							{
								System.Console.WriteLine("Image data error: {0}",i);
								continue;
							}
							byte[] array6 = new byte[spi_file_head_t.file_size];
							Buffer.BlockCopy(fileDat, ui_head_t.offset + Marshal.SizeOf((object)spi_file_head_t), array6, 0, (int)spi_file_head_t.file_size);
							if (m_ImageStream[i] != null)
							{
								m_ImageStream[i].Close();
								m_ImageStream[i].Dispose();
								m_ImageStream[i] = null;
							}
							string text2 = AppDomain.CurrentDomain.BaseDirectory + "\\" + i.ToString() + ".bmp";
							if (GetDataPicture(ui_head_t.x_width, ui_head_t.y_height, array6, text2))
							{
								ImageNum++;
								BitmapImage bitmapImage = new BitmapImage();
								m_ImageStream[i] = new FileStream(text2, FileMode.Open);
								bitmapImage.BeginInit();
								bitmapImage.StreamSource = m_ImageStream[i];
								bitmapImage.EndInit();
							}
						}
						else
						{
							System.Console.WriteLine("Image offset error:{0}",i);
							return ImageNum;
						}
					}
					else
					{
						System.Console.WriteLine("Unable to generate image:{0}",i);
						return ImageNum;
					}
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
        if (args.Length < 2)
        {
            System.Console.WriteLine("Please enter binary UI package and output folder.");
            System.Console.WriteLine("Usage: UIextractor <input UI binary> <output folder>");
            return 1;
        }

        // Get the images.
        Extractor extractor = new Extractor();
        int result = extractor.LoadUI(args[0]);

        return 0;
    }
}

