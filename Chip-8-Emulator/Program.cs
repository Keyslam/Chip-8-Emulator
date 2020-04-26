using Chip_8_Emulator.Core;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Mathematics;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Chip_8_Emulator
{
	public class Program
	{
		static void Main()
		{
			const int WINDOW_SIZE_SCALAR = 20;		

			GameWindowSettings gameWindowSettings = new GameWindowSettings();
			gameWindowSettings.RenderFrequency = 60;
			gameWindowSettings.UpdateFrequency = 60;

			NativeWindowSettings nativeWindowSettings = new NativeWindowSettings();
			nativeWindowSettings.Size = new Vector2i(64 * WINDOW_SIZE_SCALAR, 32 * WINDOW_SIZE_SCALAR);
			nativeWindowSettings.Title = "Chip 8 Emulator";

			Window window = new Window(gameWindowSettings, nativeWindowSettings);
			window.Run();
		}
	}

	public class Window : GameWindow
	{
		private Shader shader = null;
		private int textureHandle = -1;
		private int vboHandle = -1;
		private int vaoHandle = -1;

		public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings) { }

		protected override void OnLoad()
		{
			base.OnLoad();

			GL.Enable(EnableCap.DebugOutput);
			DebugProc openGLDebugDelegate = new DebugProc(OpenGLDebugCallback);
			GL.DebugMessageCallback(openGLDebugDelegate, IntPtr.Zero);

			// Create shader
			shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
			shader.Use();

			// Create target texture
			textureHandle = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, textureHandle);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			Random random = new Random();

			byte[] pixels = new byte[64 * 32];
			for (int i = 0; i < 64 * 32; i++)
			{
				pixels[i] = (byte)(random.NextDouble() * 255.0f);
			}
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, 64, 32, 0, PixelFormat.Red, PixelType.UnsignedByte, pixels);

			// Create mesh
			float[] vertices = new float[]
			{
				// vec2 position
				// vec2 texCoord

				// Top left triangle
				-1, -1,		0, 1,
				 1, -1,		1, 1,
				-1,  1,		0, 0,

				// Bottom right triangle
				 1, -1,		1, 1,
				 1,  1,		1, 0,
				-1,  1,		0, 0,
			};

			vboHandle = GL.GenBuffer();
			vaoHandle = GL.GenVertexArray();

			GL.BindVertexArray(vaoHandle);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);

			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0 * sizeof(float));

			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

			GL.BindVertexArray(vaoHandle);
			GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);
		}

		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			base.OnUpdateFrame(args);
		}

		protected override void OnRenderFrame(FrameEventArgs args)
		{
			base.OnRenderFrame(args);

			GL.Clear(ClearBufferMask.ColorBufferBit);

			GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

			SwapBuffers();
		}

		protected override void OnUnload()
		{
			base.OnUnload();

			if (shader != null)
				shader.Dispose();
		}

		private void OpenGLDebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
		{
			string msg = Marshal.PtrToStringAnsi(message, length);
			Console.WriteLine(msg);
		}
	}
}
