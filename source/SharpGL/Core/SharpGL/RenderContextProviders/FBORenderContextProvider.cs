﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpGL.Version;

namespace SharpGL.RenderContextProviders
{
    using System.Runtime.InteropServices;

    public class FBORenderContextProvider : HiddenWindowRenderContextProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FBORenderContextProvider"/> class.
        /// </summary>
        public FBORenderContextProvider()
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
            this.gl = gl;

            //  Call the base class. 	        
            base.Create(openGLVersion, gl, width, height, bitDepth, parameter);

            uint[] ids = new uint[1];

            gl.GenFramebuffers(1, ids);
            this.targetFrameBufferID = ids[0];
            gl.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, this.targetFrameBufferID);

            //	Create the colour render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffers(1, ids);
            targetColorRenderBufferId = ids[0];
            gl.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, targetColorRenderBufferId);
            gl.RenderbufferStorage(OpenGL.GL_RENDERBUFFER, OpenGL.GL_RGBA, width, height);

            //	Create the depth render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffers(1, ids);
            targetDepthRenderBufferId = ids[0];
            gl.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, targetDepthRenderBufferId);
            gl.RenderbufferStorage(OpenGL.GL_RENDERBUFFER, OpenGL.GL_DEPTH_COMPONENT24, width, height);

            //  Set the render buffer for colour and depth.
            gl.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0,
                OpenGL.GL_RENDERBUFFER, targetColorRenderBufferId);
            gl.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_DEPTH_ATTACHMENT,
                OpenGL.GL_RENDERBUFFER, targetDepthRenderBufferId);

            //  First, create the frame buffer and bind it.
            ids = new uint[1];
            gl.GenFramebuffers(1, ids);
            frameBufferID = ids[0];
            gl.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, frameBufferID);
                        
		    //	Create the colour render buffer and bind it, then allocate storage for it.
		    gl.GenRenderbuffers(1, ids);
            colourRenderBufferID = ids[0];
		    gl.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, colourRenderBufferID);
            gl.RenderbufferStorageMultisample(OpenGL.GL_RENDERBUFFER, 8, OpenGL.GL_RGBA, width, height);

            //	Create the depth render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffers(1, ids);
            depthRenderBufferID = ids[0];
            gl.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, depthRenderBufferID);
            gl.RenderbufferStorageMultisample(OpenGL.GL_RENDERBUFFER, 8, OpenGL.GL_DEPTH_COMPONENT24, width, height);

            //  Set the render buffer for colour and depth.
            gl.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0,
                OpenGL.GL_RENDERBUFFER, colourRenderBufferID);
            gl.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_DEPTH_ATTACHMENT,
                OpenGL.GL_RENDERBUFFER, depthRenderBufferID);

            dibSectionDeviceContext = Win32.CreateCompatibleDC(deviceContextHandle);
		
            //  Create the DIB section.
            dibSection.Create(dibSectionDeviceContext, width, height, bitDepth);
            
            return true;
	    }

        private void DestroyFramebuffers()
        {
            //  Delete the render buffers.
            gl.DeleteRenderbuffers(2, new uint[] { colourRenderBufferID, depthRenderBufferID });

            //	Delete the framebuffer.
            gl.DeleteFramebuffers(1, new uint[] { frameBufferID });

            //  Reset the IDs.
            colourRenderBufferID = 0;
            depthRenderBufferID = 0;
            frameBufferID = 0;
        }

        public override void Destroy()
        {
            //  Delete the render buffers.
            DestroyFramebuffers();

            //  Destroy the internal dc.
            Win32.DeleteDC(dibSectionDeviceContext);

		    //	Call the base, which will delete the render context handle and window.
            base.Destroy();
	    }

        public override void SetDimensions(int width, int height)
        {
            //  Call the base.
            base.SetDimensions(width, height);

		    //	Resize dib section.
		    dibSection.Resize(width, height, BitDepth);

            DestroyFramebuffers();

            //  TODO: We should be able to just use the code below - however we 
            //  get invalid dimension issues at the moment, so recreate for now.
            //  TODO: DK: Quick tests show this works now we've correctly mapped the FBO extensions, however check
            //  carefully before changing implementation.

            //  Resize the render buffer storage.
            //gl.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, colourRenderBufferID);
            //gl.RenderbufferStorage(OpenGL.GL_RENDERBUFFER, OpenGL.GL_RGBA, width, height);
            //gl.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, depthRenderBufferID);
            //gl.RenderbufferStorage(OpenGL.GL_RENDERBUFFER, OpenGL.GL_DEPTH_ATTACHMENT, width, height);
            //var complete = gl.CheckFramebufferStatus(OpenGL.GL_FRAMEBUFFER);


            uint[] ids = new uint[1];

            gl.GenFramebuffers(1, ids);
            this.targetFrameBufferID = ids[0];
            gl.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, this.targetFrameBufferID);

            //	Create the colour render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffers(1, ids);
            targetColorRenderBufferId = ids[0];
            gl.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, targetColorRenderBufferId);
            gl.RenderbufferStorage(OpenGL.GL_RENDERBUFFER, OpenGL.GL_RGBA, width, height);

            //	Create the depth render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffers(1, ids);
            targetDepthRenderBufferId = ids[0];
            gl.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, targetDepthRenderBufferId);
            gl.RenderbufferStorage(OpenGL.GL_RENDERBUFFER, OpenGL.GL_DEPTH_COMPONENT24, width, height);

            //  Set the render buffer for colour and depth.
            gl.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0,
                OpenGL.GL_RENDERBUFFER, targetColorRenderBufferId);
            gl.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_DEPTH_ATTACHMENT,
                OpenGL.GL_RENDERBUFFER, targetDepthRenderBufferId);

            //  First, create the frame buffer and bind it.
            ids = new uint[1];
            gl.GenFramebuffers(1, ids);
            frameBufferID = ids[0];
            gl.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, frameBufferID);

            //	Create the colour render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffers(1, ids);
            colourRenderBufferID = ids[0];
            gl.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, colourRenderBufferID);
            gl.RenderbufferStorageMultisample(OpenGL.GL_RENDERBUFFER, 8, OpenGL.GL_RGBA, width, height);

            //	Create the depth render buffer and bind it, then allocate storage for it.
            gl.GenRenderbuffers(1, ids);
            depthRenderBufferID = ids[0];
            gl.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, depthRenderBufferID);
            gl.RenderbufferStorageMultisample(OpenGL.GL_RENDERBUFFER, 8, OpenGL.GL_DEPTH_COMPONENT24, width, height);

            //  Set the render buffer for colour and depth.
            gl.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0,
                OpenGL.GL_RENDERBUFFER, colourRenderBufferID);
            gl.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_DEPTH_ATTACHMENT,
                OpenGL.GL_RENDERBUFFER, depthRenderBufferID);
        }

        public override void Blit(IntPtr hdc)
        {
            if (deviceContextHandle != IntPtr.Zero)
            {
                gl.BindFramebuffer(OpenGL.GL_READ_FRAMEBUFFER, this.frameBufferID);
                gl.BindFramebuffer(OpenGL.GL_DRAW_FRAMEBUFFER, this.targetFrameBufferID);
                gl.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, OpenGL.GL_COLOR_BUFFER_BIT, OpenGL.GL_NEAREST);
                gl.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, this.targetFrameBufferID);
                ////  Set the read buffer.
                gl.ReadBuffer(OpenGL.GL_COLOR_ATTACHMENT0);

                //gl.BlitBuffer

			    //	Read the pixels into the DIB section.
			    gl.ReadPixels(0, 0, Width, Height, OpenGL.GL_BGRA, 
                    OpenGL.GL_UNSIGNED_BYTE, dibSection.Bits);

			    //	Blit the DC (containing the DIB section) to the target DC.
			    Win32.BitBlt(hdc, 0, 0, Width, Height,
                    dibSectionDeviceContext, 0, 0, Win32.SRCCOPY)
                    ;
                gl.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, this.frameBufferID);
            }
        }

        protected uint colourRenderBufferID = 0;
        protected uint depthRenderBufferID = 0;
        protected uint frameBufferID = 0;

        protected uint targetColorRenderBufferId = 0;
        protected uint targetDepthRenderBufferId = 0;
        protected uint targetFrameBufferID = 0;
        protected IntPtr dibSectionDeviceContext = IntPtr.Zero;
        protected DIBSection dibSection = new DIBSection();
        protected OpenGL gl;

        /// <summary>
        /// Gets the internal DIB section.
        /// </summary>
        /// <value>The internal DIB section.</value>
        public DIBSection InternalDIBSection
        {
            get { return dibSection; }
        }
    }
}
