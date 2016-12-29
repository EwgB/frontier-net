using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using NLog;

namespace FrontierSharp {
	internal class Frontier : GameWindow {

		private static Logger logger = LogManager.GetCurrentClassLogger();

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
			logger.Trace("OnLoad");

			Title = "Frontier";
			GL.ClearColor(Color.CornflowerBlue);
		}

		protected override void OnRenderFrame(FrameEventArgs e) {
			base.OnRenderFrame(e);
			logger.Trace("OnRender");

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			var modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref modelview);

			GL.Begin(PrimitiveType.Triangles);
			{
				GL.Color3(1.0f, 0.0f, 0.0f);
				GL.Vertex3(-1.0f, -1.0f, 4.0f);

				GL.Color3(0.0f, 1.0f, 0.0f);
				GL.Vertex3(1.0f, -1.0f, 4.0f);

				GL.Color3(0.0f, 0.0f, 1.0f);
				GL.Vertex3(0.0f, 1.0f, 4.0f);
			}
			GL.End();

			SwapBuffers();
		}

		protected override void OnResize(EventArgs e) {
			base.OnResize(e);

			GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
			var projection = Matrix4.CreatePerspectiveFieldOfView((float) Math.PI / 4, Width / (float) Height, 1.0f, 64.0f);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref projection);
		}

	}
}
