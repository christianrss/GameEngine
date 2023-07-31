using System;
using System.Collections;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using Microsoft.DirectX.Direct3D;
using System.Threading;
using System.Diagnostics;

namespace GameEngine
{

    /// <summary>
    /// Summary description for GameEngine.
    /// </summary>
    #region delegates
    public delegate void BackgroundTask();
    #endregion

    public class CGameEngine : IDisposable
    {
        #region Attributes
        // A local reference to the DirectX device
        private static Microsoft.DirectX.Direct3D.Device m_pd3dDevice;
        private System.Windows.Forms.Form m_WindForm;
        private SplashScreen m_SplashScreen = null;
        private OptionScreen m_OptionScreen = null;
        private SkyBox m_SkyBox = null;
        private Camera m_Camera = null;
        public static GameInput m_GameInput = null;
        public static Terrain m_Terrain = null;
        public static Quad m_QuadTree = null;
        private ArrayList m_Objects = null;
        private ArrayList m_Cameras = null;
        public float fTimeLeft = 0.0f;
        Thread m_ThreadTask = null;
        #endregion

        #region Properties
        public Camera Cam { get { return m_Camera; } }
        public ArrayList Objects { get { return m_Objects; } }
        public static Terrain Ground { get { return m_Terrain; } }
        public static Quad QuadTree { get { return m_QuadTree; } }
        public static Microsoft.DirectX.Direct3D.Device Device3D { get { return m_pd3dDevice; } }
        public static GameInput Inputs { get { return m_GameInput;  } }
        public static Color FogColor { set { m_pd3dDevice.RenderState.FogColor = value; } }
        public static FogMode FogTableMode { set { m_pd3dDevice.RenderState.FogTableMode = value;  } }
        public static FogMode FogVertexMode { set { m_pd3dDevice.RenderState.FogVertexMode = value; } }
        public static float FogDensity { set { m_pd3dDevice.RenderState.FogDensity = value;  } }
        public static float FogStart { set { m_pd3dDevice.RenderState.FogStart = value;  } }
        public static float FogEnd { set { m_pd3dDevice.RenderState.FogEnd = value;  } }
        public static bool FogEnable { set { m_pd3dDevice.RenderState.FogEnable = value;  } }
        #endregion

        int frame = 0;

        public void Dispose()
        {
            Debug.WriteLine("disposing of game engine objects");
       
        }
    }
}