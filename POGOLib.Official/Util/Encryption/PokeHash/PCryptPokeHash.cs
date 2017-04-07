using System;

namespace POGOLib.Official.Util.Encryption.PokeHash
{
    /// <summary>
    ///     This is the PCrypt used by POGOLib. It should always match the used PokeHash version.
    /// 
    ///     Android version: 0.59.1
    ///     IOS version: 1.29.1
    /// </summary>
    internal static class PCryptPokeHash
    {
		public static byte[] KEY = new byte[]
		{
			(byte) 0x4F, (byte) 0xEB, (byte) 0x1C, (byte) 0xA5, (byte) 0xF6, (byte) 0x1A, (byte) 0x67, (byte) 0xCE,
			(byte) 0x43, (byte) 0xF3, (byte) 0xF0, (byte) 0x0C, (byte) 0xB1, (byte) 0x23, (byte) 0x88, (byte) 0x35,
			(byte) 0xE9, (byte) 0x8B, (byte) 0xE8, (byte) 0x39, (byte) 0xD8, (byte) 0x89, (byte) 0x8F, (byte) 0x5A,
			(byte) 0x3B, (byte) 0x51, (byte) 0x2E, (byte) 0xA9, (byte) 0x47, (byte) 0x38, (byte) 0xC4, (byte) 0x14
		};
			
		public static byte[] MakeIv(Rand rand)
		{
			byte[] iv = new byte[TwoFish.BLOCK_SIZE];
			for (int i = 0; i < iv.Length; i++)
			{
				iv[i] = rand.Next();
			}
			return iv;
		}

		public static byte MakeIntegrityByte(Rand rand)
		{
			return 0x21;
		}

		/**
			* Encrypts the given signature
			*
			* @param input input data
			* @param msSinceStart time since start
			* @return encrypted signature
			*/
		public static byte[] Encrypt(byte[] input, uint msSinceStart)
		{
			try
			{
				object[] key = TwoFish.MakeKey(KEY);

				Rand rand = new Rand(msSinceStart);
				byte[] iv = MakeIv(rand);
				int blockCount = (input.Length + 256) / 256;
				int outputSize = (blockCount * 256) + 5;
				byte[] output = new byte[outputSize];

				output[0] = (byte)(msSinceStart >> 24);
				output[1] = (byte)(msSinceStart >> 16);
				output[2] = (byte)(msSinceStart >> 8);
				output[3] = (byte)msSinceStart;

				Array.Copy(input, 0, output, 4, input.Length);
				output[outputSize - 2] = (byte)(256 - input.Length % 256);

				for (int offset = 0; offset < blockCount * 256; offset += TwoFish.BLOCK_SIZE)
				{
					for (int i = 0; i < TwoFish.BLOCK_SIZE; i++)
					{
						output[4 + offset + i] ^= iv[i];
					}

					byte[] block = TwoFish.blockEncrypt(output, offset + 4, key);
					Array.Copy(block, 0, output, offset + 4, block.Length);
					Array.Copy(output, 4 + offset, iv, 0, TwoFish.BLOCK_SIZE);
				}

				output[outputSize - 1] = MakeIntegrityByte(rand);
				return output;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public class Rand
		{
			private long state;

			public Rand(long state)
			{
				this.state = state;
			}

			public byte Next()
			{
				state = (state * 0x41C64E6D) + 0x3039;
				return (byte)((state >> 16) & 0xFF);
			}
		}
    }
}
