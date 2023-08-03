using System;
using System.Collections;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using Microsoft.DirectX.Direct3D;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;

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
        private System.Windows.Forms.Form m_WinForm;
        private SplashScreen m_SplashScreen = null;
        private OptionScreen m_OptionScreen = null;
        private SkyBox m_Skybox = null;
        private Camera m_Camera = null;
        public static GameInput m_GameInput = null;
        public static Terrain m_Terrain = null;
        public static Quad m_QuadTree = null;
        private ArrayList m_Objects = null;
        private ArrayList m_Cameras = null;
        public float fTimeLeft = 0.0f;
        Thread m_threadTask = null;
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
            m_GameInput.Dispose();
            Debug.WriteLine("disposing of terrain");
            if (m_Terrain != null) m_Terrain.Dispose();
            Debug.WriteLine("disposing of skybox");
            m_Skybox.Dispose();
            Debug.WriteLine("disposing of quadtree");
            m_QuadTree.Dispose();
            Debug.WriteLine("disposing of splashscreen");
            if (m_SplashScreen != null)
            {
                m_SplashScreen.Dispose();
            }
            Debug.WriteLine("disposing of optionscreen");
            if (m_OptionScreen != null)
            {
                m_OptionScreen.Dispose();
            }
            Debug.WriteLine("number of objects=" + m_Objects.Count);
            for (int i = 0; i < m_Objects.Count; i++)
            {
                try
                {
                    Object3D obj = (Object3D)m_Objects[i];
                    Debug.WriteLine("calling dispose for " + obj.Name);
                    obj.Dispose();
                }
                catch
                {

                }
            }
            for (int i = 0; i < BillBoard.Objects.Count; i++)
            {
                Object3D obj = (Object3D)BillBoard.Objects[i];
                obj.Dispose();
            }
        }

        public void RestoreSurfaces()
        {
            if (m_SplashScreen != null)
            {
                m_SplashScreen.Restore();
            }
            if (m_OptionScreen != null)
            {
                m_OptionScreen.Restore();
            }
        }

        public void SetOptionScreen(OptionScreen Screen)
        {
            m_OptionScreen = Screen;
        }

        /// <summary>
        /// Initial setup of the Game Engine
        /// </summary>
        /// <param name="="pd3dDevice"></param>
        public async void Initialize (System.Windows.Forms.Form form, Microsoft.DirectX.Direct3D.Device pd3dDevice)
        {
            // capture a reference to the window handle
            m_WinForm = form;
            // catpure a reference to the window handle
            // for now just capture a reference to the Directx device for future
            m_pd3dDevice = pd3dDevice;
            m_GameInput = new GameInput(m_WinForm);

            m_Skybox = new SkyBox(
                @"..\..\Resources\Dunes_Front.tga",
                @"..\..\Resources\Dunes_Right.tga",
                @"..\..\Resources\Dunes_Back.tga",
                @"..\..\Resources\Dunes_Left.tga",
                @"..\..\Resources\Dunes_Top.tga",
                @"..\..\Resources\Dunes_Bottom.tga"
            );

            m_Camera = new Camera();
            m_Cameras = new ArrayList();
            m_Cameras.Add(m_Camera);

            m_Objects = new ArrayList();

            m_pd3dDevice.RenderState.Ambient = System.Drawing.Color.Gray;
            
            // Set light #0 to be a simples, faint grey directional light so
            // the walls and floor are slightly different shades of gray
            m_pd3dDevice.RenderState.Lighting = true; // was true

            //GameLights.InitializeLights();

        }

        /// <summary>
        /// Display a Splash Screen based on a supplied bitmap filenam
        /// </summary>
        /// <param name="sFileName"></param>
        public bool ShowSplash(string sFileName, int nSeconds, BackgroundTask task)
        {
            bool bDone = false;
            if (m_SplashScreen == null)
            {
                m_SplashScreen = new SplashScreen(sFileName, nSeconds);

                if (task != null)
                {
                    m_threadTask = new Thread(new ThreadStart(task));
                    m_threadTask.Name = "Game_backgroundTask";
                    m_threadTask.Start();
                }
            }

            bDone = m_SplashScreen.Render();

            fTimeLeft = m_SplashScreen.fTimeLeft;

            if (bDone)
            {
                m_SplashScreen.Dispose();
                m_SplashScreen = null;
            }

            return bDone;
        }

        /// <summary>
        /// Display the Options screen
        /// </summary>
        public void DoOptions()
        {
            if (m_OptionScreen != null)
            {
                m_OptionScreen.SetMousePosition(
                    m_GameInput.GetMousePoint().X,
                    m_GameInput.GetMousePoint().Y,
                    m_GameInput.IsMouseButtonDown(0)
                 );
                m_OptionScreen.Render();
            }
        }

        /// <summary>
        /// Display the latest game frame
        /// </summary>
        public void Render()
        {
            m_Camera.Render();
            m_QuadTree.Cull(m_Camera);
            // GameLights.CheckCulling(m_Camera);

            // test code
            //Model ownship = (Model)GetObject("car1");
            //if (ownship != null && ownship.IsCulled) {
            //    Console.AddLine("ownship culled at " + ownship.North + " " + ownship.East + " H " + ownship.Heading);
            //}

            //GameLights.DeactivateLights();

            if (m_Skybox != null)
            {
                m_Skybox.Render(m_Camera);
            }

            //GameLights.SetupLights();

            if (m_Terrain != null)
            {
                m_Terrain.Render(m_Camera);
            }

            BillBoard.RenderAll(m_Camera);

            foreach (Object3D obj in m_Objects)
            {
                if (!obj.IsCulled)
                {
                    obj.Render(m_Camera);
                }
            }
        }

        /// <summary>
        /// Process mouse, keyboard and if appropriate, joystick inputs
        /// </summary>
        public void GetPlayerInputs()
        {
            m_GameInput.Poll();
        }

        /// <summary>
        /// Process any automated player artificial intelligence
        /// </summary>
        /// <param name="DeltaT"></param>
        public void DoAI(float DeltaT)
        {

        }

        /// <summary>
        /// Process any moving object dynamics
        /// </summary>
        /// <param name="DeltaT"></param>
        public void DoDynamics(float DeltaT)
        {
            try
            {
                frame++;
                if (frame > 30)
                {
                    if (m_Terrain != null)
                    {
                        bool los = m_Terrain.InLineOfSight(
                            new Vector3(0.0f, 1.0f, 0.0f),
                            m_Camera.EyeVector
                        );
                        frame = 0;
                        Console.AddLine("los = " + los);
                    }
                }
                if (m_Objects.Count > 0)
                {
                    foreach(Object3D obj in m_Objects)
                    {
                        obj.Update(DeltaT);
                    }
                }
            }
            catch (DirectXException d3de)
            {
                Console.AddLine("Unable to update an object " + m_Objects);
                Console.AddLine(d3de.ErrorString);
            }
            catch (Exception e)
            {
                Console.AddLine("Unable to update an object " + m_Objects);
                Console.AddLine(e.Message);
            }
        }

        public void MoveCamera(float x, float y, float z, float pitch, float roll, float heading)
        {
            m_Camera.AdjustHeading(heading);
            m_Camera.AdjustPitch(pitch);
            m_Camera.MoveCamera(x, y, z);
        }

        public void SetTerrain(int xSize, int ySize, string sName, string sTexture, float fSpacing, float fElevFactor)
        {
            Rectangle bounds = new Rectangle(0, 0, (int)(xSize * fSpacing + 0.9), (int)(ySize * fSpacing + 0.9));
            m_QuadTree = new Quad(bounds, 0, 7, null);
            m_Terrain = new Terrain(xSize, ySize, sName, sTexture, fSpacing, fElevFactor);
        }

        public void AddObject(Object3D obj)
        {
            Debug.WriteLine("adding " + obj.Name + " to engine object list");
            m_QuadTree.AddObject(obj);
            m_Objects.Add(obj);
        }

        public Object3D GetObject (string name)
        {
            Object3D obj = null;
            foreach (Object3D o in m_Objects)
            {
                if (o.Name == name)
                {
                    obj = o;
                }
            }
            if (obj == null)
            {
                foreach (Object3D o in BillBoard.Objects)
                {
                    if (o.Name == name)
                    {
                        obj = o;
                    }
                }
            }
            return obj;
        }

        public bool SetCamera(string name)
        {
            bool success = false;
            foreach (Camera c in m_Cameras)
            {
                if (c.Name == name)
                {
                    m_Camera = c;
                    success = true;
                }
            }
            return success;
        }

        public void AddCamera(Camera cam)
        {
            m_Cameras.Add(cam);
        }

        public void RemoveCamera(string name)
        {
            Camera cam = null;
            foreach (Camera c in m_Cameras)
            {
                if (c.Name == name)
                {
                    cam = c;
                    break;
                }
            }
            if (cam != null) m_Cameras.Remove(cam);
        }
    }
}