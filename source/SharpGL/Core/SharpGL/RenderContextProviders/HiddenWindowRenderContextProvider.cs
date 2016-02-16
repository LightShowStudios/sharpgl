using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpGL.Version;

namespace SharpGL.RenderContextProviders
{
    public class HiddenWindowRenderContextProvider : RenderContextProvider
    {
        private bool arbMultisampleSupported;
        private int arbMultisampleFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="HiddenWindowRenderContextProvider"/> class.
        /// </summary>
        public HiddenWindowRenderContextProvider()
        {
            //  We can layer GDI drawing on top of open gl drawing.
            GDIDrawingEnabled = true;
        }

        /// <summary>
        /// Creates the render context provider. Must also create the OpenGL extensions.
        /// </summary>
        /// <param name="openGLVersion">The desired OpenGL version.</param>
        /// <param name="gl">The OpenGL context.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="bitDepth">The bit depth.</param>
        /// <param name="parameter">The parameter</param>
        /// <returns></returns>
        public override bool Create(OpenGLVersion openGLVersion, OpenGL gl, int width, int height, int bitDepth, object parameter)
        {
            //  Call the base.
            base.Create(openGLVersion, gl, width, height, bitDepth, parameter);

            //	Create a new window class, as basic as possible.                
            Win32.WNDCLASSEX wndClass = new Win32.WNDCLASSEX();
            wndClass.Init();
		    wndClass.style			= Win32.ClassStyles.HorizontalRedraw | Win32.ClassStyles.VerticalRedraw | Win32.ClassStyles.OwnDC;
            wndClass.lpfnWndProc    = wndProcDelegate;
		    wndClass.cbClsExtra		= 0;
		    wndClass.cbWndExtra		= 0;
		    wndClass.hInstance		= IntPtr.Zero;
		    wndClass.hIcon			= IntPtr.Zero;
		    wndClass.hCursor		= IntPtr.Zero;
		    wndClass.hbrBackground	= IntPtr.Zero;
		    wndClass.lpszMenuName	= null;
		    wndClass.lpszClassName	= "SharpGLRenderWindow";
		    wndClass.hIconSm		= IntPtr.Zero;
		    Win32.RegisterClassEx(ref wndClass);
            	
		    //	Create the window. Position and size it.
		    windowHandle = Win32.CreateWindowEx(0,
					      "SharpGLRenderWindow",
					      "",
					      Win32.WindowStyles.WS_CLIPCHILDREN | Win32.WindowStyles.WS_CLIPSIBLINGS | Win32.WindowStyles.WS_POPUP,
					      0, 0, width, height,
					      IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

		    //	Get the window device context.
		    deviceContextHandle = Win32.GetDC(windowHandle);

		    //	Setup a pixel format.
		    Win32.PIXELFORMATDESCRIPTOR pfd = new Win32.PIXELFORMATDESCRIPTOR();
            pfd.Init();
		    pfd.nVersion = 1;
		    pfd.dwFlags = Win32.PFD_DRAW_TO_WINDOW | Win32.PFD_SUPPORT_OPENGL | Win32.PFD_DOUBLEBUFFER;
		    pfd.iPixelType = Win32.PFD_TYPE_RGBA;
		    pfd.cColorBits = (byte)bitDepth;
		    pfd.cDepthBits = 16;
		    pfd.cStencilBits = 8;
		    pfd.iLayerType = Win32.PFD_MAIN_PLANE;
		
		    //	Match an appropriate pixel format 
		    int iPixelformat;
            if (!this.arbMultisampleSupported)
            {
                if ((iPixelformat = Win32.ChoosePixelFormat(deviceContextHandle, pfd)) == 0)
                    return false;
            }
            else
            {
                iPixelformat = arbMultisampleFormat;
            }

            //	Sets the pixel format
            if (Win32.SetPixelFormat(deviceContextHandle, iPixelformat, pfd) == 0)
		    {
			    return false;
		    }

		    //	Create the render context.
            renderContextHandle = Win32.wglCreateContext(deviceContextHandle);
            
            //  Make the context current.
            MakeCurrent();

            //if (!arbMultisampleSupported)
            //{
            //    if (InitMultisample(deviceContextHandle, gl, pfd))
            //    {
            //        this.Destroy();
            //        return this.Create(openGLVersion, gl, width, height, bitDepth, parameter);
            //    }
            //}

            //  Update the context if required.
            UpdateContextVersion(gl);

            //  Return success.
            return true;
        }

        // WGLisExtensionSupported: This Is A Form Of The Extension For WGL
        private bool WGLisExtensionSupported(OpenGL gl, string extension)
        {

            var extlen = extension.Length;
            string supported = null;

            supported = gl.GetExtensionsStringARB();
            // Try To Use wglGetExtensionStringARB On Current DC, If Possible

            // If That Failed, Try Standard Opengl Extensions String
            if (supported == null) supported = gl.GetString(OpenGL.GL_EXTENSIONS);

            // If That Failed Too, Must Be No Extensions Supported
            if (supported == null) return false;

            return true;
            // Begin Examination At Start Of String, Increment By 1 On False Match
        }

        // InitMultisample: Used To Query The Multisample Frequencies
private bool InitMultisample(IntPtr hdc, OpenGL gl, Win32.PIXELFORMATDESCRIPTOR pfd)
        {
            //See If The String Exists In WGL!
            if (!this.WGLisExtensionSupported(gl, "WGL_ARB_multisample"))
            {
                arbMultisampleSupported = false;
                return false;
            }

            //// Get Our Pixel Format
            //PFNWGLCHOOSEPIXELFORMATARBPROC wglChoosePixelFormatARB = (PFNWGLCHOOSEPIXELFORMATARBPROC)wglGetProcAddress("wglChoosePixelFormatARB");
            //if (!wglChoosePixelFormatARB)
            //{
            //    arbMultisampleSupported = false;
            //    return false;
            //}

            // Get Our Current Device Context
            //HDC hDC = GetDC(hWnd);

            int pixelFormat = 0;
            bool valid;
            uint numFormats = 0;
            float[] fAttributes = {0f, 0f};

            // These Attributes Are The Bits We Want To Test For In Our Sample
            // Everything Is Pretty Standard, The Only One We Want To 
            // Really Focus On Is The SAMPLE BUFFERS ARB And WGL SAMPLES
            // These Two Are Going To Do The Main Testing For Whether Or Not
            // We Support Multisampling On This Hardware.
            int[] iAttributes =
            {
                (int) OpenGL.WGL_DRAW_TO_WINDOW_ARB, (int) OpenGL.GL_TRUE,
                (int) OpenGL.WGL_SUPPORT_OPENGL_ARB, (int) OpenGL.GL_TRUE,
                (int) OpenGL.WGL_ACCELERATION_ARB, (int) OpenGL.WGL_FULL_ACCELERATION_ARB,
                (int) OpenGL.WGL_COLOR_BITS_ARB, 24,
                (int) OpenGL.WGL_ALPHA_BITS_ARB, 8,
                (int) OpenGL.WGL_DEPTH_BITS_ARB, 16,
                (int) OpenGL.WGL_STENCIL_BITS_ARB, 0,
                (int) OpenGL.WGL_DOUBLE_BUFFER_ARB, (int) OpenGL.GL_TRUE,
                (int) OpenGL.WGL_SAMPLE_BUFFERS_ARB, (int) OpenGL.GL_TRUE,
                (int) OpenGL.WGL_SAMPLES_ARB, 4,
                0, 0
            };

            // First We Check To See If We Can Get A Pixel Format For 4 Samples
            valid = gl.ChoosePixelFormatARB(hdc, iAttributes, fAttributes, 1, ref pixelFormat, ref numFormats);

            // If We Returned True, And Our Format Count Is Greater Than 1
            if (valid && numFormats >= 1)
            {
                arbMultisampleSupported = true;
                arbMultisampleFormat = pixelFormat;
                return arbMultisampleSupported;
            }

            // Our Pixel Format With 4 Samples Failed, Test For 2 Samples
            iAttributes[19] = 2;
            valid = gl.ChoosePixelFormatARB(hdc, iAttributes, fAttributes, 1, ref pixelFormat, ref numFormats);
            if (valid && numFormats >= 1)
            {
                arbMultisampleSupported = true;
                arbMultisampleFormat = pixelFormat;
                return arbMultisampleSupported;
            }

            // Return The Valid Format
            return arbMultisampleSupported;
        }

        private static Win32.WndProc wndProcDelegate = new Win32.WndProc(WndProc);

        static private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return Win32.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void DestroyWindow()
        {
            //	Release the device context.
            Win32.ReleaseDC(windowHandle, deviceContextHandle);

            //	Destroy the window.
            Win32.DestroyWindow(windowHandle);
        }

        /// <summary>
        /// Destroys the render context provider instance.
        /// </summary>
	    public override void Destroy()
	    {
		    this.DestroyWindow();

		    //	Call the base, which will delete the render context handle.
            base.Destroy();
	    }

        /// <summary>
        /// Sets the dimensions of the render context provider.
        /// </summary>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
	    public override void SetDimensions(int width, int height)
	    {
            //  Call the base.
            base.SetDimensions(width, height);

		    //	Set the window size.
            Win32.SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, Width, Height, 
                Win32.SetWindowPosFlags.SWP_NOACTIVATE | 
                Win32.SetWindowPosFlags.SWP_NOCOPYBITS | 
                Win32.SetWindowPosFlags.SWP_NOMOVE | 
                Win32.SetWindowPosFlags.SWP_NOOWNERZORDER);
	    }

        /// <summary>
        /// Blit the rendered data to the supplied device context.
        /// </summary>
        /// <param name="hdc">The HDC.</param>
	    public override void Blit(IntPtr hdc)
	    {
		    if(deviceContextHandle != IntPtr.Zero || windowHandle != IntPtr.Zero)
		    {
			    //	Swap the buffers.
                Win32.SwapBuffers(deviceContextHandle);
			    
			    //  Get the HDC for the graphics object.
                Win32.BitBlt(hdc, 0, 0, Width, Height, deviceContextHandle, 0, 0, Win32.SRCCOPY);
		    }
	    }

        /// <summary>
        /// Makes the render context current.
        /// </summary>
	    public override void MakeCurrent()
	    {
		    if(renderContextHandle != IntPtr.Zero)
			    Win32.wglMakeCurrent(deviceContextHandle, renderContextHandle);
	    }

        /// <summary>
        /// The window handle.
        /// </summary>
        protected IntPtr windowHandle = IntPtr.Zero;
    }
}
