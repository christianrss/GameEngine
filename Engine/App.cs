using System;
using System.Drawing;
using System.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;

namespace GameEngine
{
    /// <summary>
    /// Summary description for GameEngine.
    /// </summary>
    class CGameApplication : GraphicsSample
    {
        #region // Game State enumeration
        /// <summary>
        /// Each member of this enumeration is one possible state for the application
        /// </summary>
        public enum GameState
        {
            DevSplash,
            GameSplash,
            OptionsMain,
            GamePlay,
            AfterActionReview
        }
        #endregion

        #region // Application member variables
        /// <summary>
        /// Current state of the application
        /// </summary>
        private GameState m_State;
        private static CGameEngine m_Engine = new CGameEngine();
        private GraphicsFont m_pFont = null;
        private GameEngine.Console m_Console;
        private ArrayList m_opponents = null;
        private OptionScreen m_OptionScreen = null;
        private bool m_bShowStatistics = false;
        private bool m_bScreenCapture = false;
        private bool m_bUsingJoystick = true;
        private bool m_bUsingKeyboard = false;
        private bool m_bUsingMouse = false;
//        private Ownship m_ownship = null;
//        private Cloth m_flag = null;
//        private Jukebox music = null;
        #endregion

        public static CGameEngine Engine { get { return m_Engine; } }

        public CGameApplication()
        {
            m_State = GameState.DevSplash;

            m_pFont = new GraphicsFont("Arial", System.Drawing.FontStyle.Bold);
            windowed = false;
            m_opponents = new ArrayList();
        }

        /// <summary>
        /// Called during initial app startup, this function performs all the
        /// permanent initialization
        /// </summary>
        protected override void OneTimeSceneInitialization()
        {
            // Initialize the font's internal textures
            m_pFont.InitializeDeviceObjects(device);

            m_Engine.Initialize(this, device);

            CGameEngine.Inputs.MapKeyboardAction(Key.Escape, new ButtonAction(Terminate), true);
            CGameEngine.Inputs.MapKeyboardAction(Key.Return, new ButtonAction(Play), true);
            CGameEngine.Inputs.MapKeyboardAction(Key.A, new ButtonAction(MoveCameraXM), false);
            CGameEngine.Inputs.MapKeyboardAction(Key.W, new ButtonAction(MoveCameraZP), false);
            CGameEngine.Inputs.MapKeyboardAction(Key.S, new ButtonAction(MoveCameraXP), false);
            CGameEngine.Inputs.MapKeyboardAction(Key.Z, new ButtonAction(MoveCameraZM), false);
            CGameEngine.Inputs.MapKeyboardAction(Key.P, new ButtonAction(ScreenCapture), false);

            CGameEngine.Inputs.MapMouseAxisAction(0, new AxisAction(PointCamera));
            CGameEngine.Inputs.MapMouseAxisAction(1, new AxisAction(PitchCamera));

            m_Console = new GameEngine.Console(m_pFont, @"..\..\Resources\console.jpg");

            GameEngine.Console.AddCommand("QUIT", "Terminate the game", new CommandFunction(TerminateCommand));
            GameEngine.Console.AddCommand("STATISTICS", "Toggle statistics display", new CommandFunction(ToggleStatistics));

            m_OptionScreen = new OptionScreen(@"..\..\Resources\Option2.jpg");
            m_OptionScreen.AddButton(
                328,
                150,
                @"..\..\Resources\PlayOff.bmp",
                @"..\..\Resouces\PlayOn.bmp",
                @"..\..\Resources\PlayHover.bmp",
                new ButtonFunction(Play)
            );
            m_OptionScreen.AddButton(
                328,
                300,
                @"..\..\Resources\QuitOff.bmp",
                @"..\..\Resouces\QuitOn.bmp",
                @"..\..\Resources\QuitHover.bmp",
                new ButtonFunction(Terminate)
            );

            m_Engine.SetOptionScreen(m_OptionScreen);
        }

        /// <summary>
        /// Called once per frame, the call is the entry point for all game processing.
        /// This functions calls the appropriate part of the game engine based on the current state.
        /// </summary>
        protected override void FrameMove()
        {
            try
            {
                SelectControls select_form = null;
                // get any player inputs
                m_Engine.GetPlayerInputs();

                // Clear the viewport
                device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, 0x0000000, 1.0f, 0);
                device.BeginScene();

                // determine what needs to be rendered base on the current game state
                switch (m_State)
                {
                    case GameState.DevSplash:
                        if (m_Engine.ShowSplash(@"..\..\Resources\devsplash.jpg", 8, new BackgroundTask(LoadOptions)))
                        {
                            m_State = GameState.GameSplash;
                        }
                        break;
                    case GameState.GameSplash:
                        if (m_Engine.ShowSplash(@"..\..\Resources\gamesplash.jpg", 8, null))
                        {
                            m_State = GameState.OptionsMain;
                            select_form = new SelectControls();
                            select_form.ShowDialog(this);
                            m_bUsingJoystick = select_form.UseJoystick.Checked;
                            m_bUsingKeyboard = select_form.UseKeyboard.Checked;
                            m_bUsingMouse = select_form.UseMouse.Checked;
                        }
                        break;
                    case GameState.OptionsMain:
                        m_Engine.DoOptions();
                        break;
                    case GameState.GamePlay:
                        m_Engine.GetPlayerInputs();
                        m_Engine.DoDynamics(elapsedTime);
                        m_Engine.Render();
                        break;
                    case GameState.AfterActionReview:
                        break;
                }

                GameEngine.Console.Render();

                // Output statistics
                if (m_bShowStatistics)
                {
                    m_pFont.DrawText(2, 560, Color.FromArgb(255, 255, 255, 0), frameStats);
                    m_pFont.DrawText(2, 580, Color.FromArgb(255, 255, 255, 0), deviceStats);
                    m_pFont.DrawText(
                        500,
                        580,
                        Color.FromArgb(255, 255, 255, 0),
                        m_Engine.Cam.Heading.ToString() + " " + m_Engine.Cam.Pitch.ToString() + " " +
                            m_Engine.Cam.X + " " + m_Engine.Cam.Y + " " + m_Engine.Cam.Z
                    );
                    m_pFont.DrawText(
                        2,
                        600,
                        Color.FromArgb(255, 255, 255, 0),
                        "Steering" + (CGameEngine.Inputs.GetJoystickX() - 1.0f).ToString() + " " +
                        "throttle/Brake " + (1.0f - CGameEngine.Inputs.GetJoystickNormalY()).ToString()
                    );
                }

                if (m_bScreenCapture)
                {
                    SurfaceLoader.Save("capture.bmp", ImageFileFormat.Bmp, device.GetBackBuffer(0, 0, BackBufferType.Mono));
                    m_bScreenCapture = false;
                    GameEngine.Console.AddLine("snapshot taken");
                }
            }
            catch (DirectXException d3de)
            {
                System.Diagnostics.Debug.WriteLine("Error in Sample Game Application FrameMove");
                System.Diagnostics.Debug.WriteLine(d3de.ErrorString);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error in Sample Game Application FrameMove");
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            finally
            {
                device.EndScene();
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                CGameApplication d3dApp = new CGameApplication();
                if (d3dApp.CreateGraphicsSample())
                {
                    d3dApp.Run();
                }
            }
            catch (DirectXException d3de)
            {
                System.Diagnostics.Debug.WriteLine("Error in Sample Game Application");
                System.Diagnostics.Debug.WriteLine(d3de.ErrorString);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error in Sample Game Application");
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        // action functions

        /// <summary>
        /// Action to start playing
        /// </summary>
        public void Play()
        {
            m_State = GameState.GamePlay;
            GameEngine.Console.Reset();
        }

        /// <summary>
        /// Action to terminate the application
        /// </summary>
        public void Terminate()
        {
            m_bTerminate = true;
        }

        /// <summary>
        /// screen capture
        /// </summary>
        public void ScreenCapture()
        {
            m_bScreenCapture = true;
        }

        /// <summary>
        /// version of terminate for use by the console
        /// </summary>
        /// <param name="sData"></param>
        public void TerminateCommand(string sData)
        {
            Terminate();
        }

        /// <summary>
        /// Toggle the display of statistics information
        /// </summary>
        /// <param name="sData"></param>
        public void ToggleStatistics(string sData)
        {
            m_bShowStatistics = !m_bShowStatistics;
        }

        /// <summary>
        /// Action to transition to the next game state base on a mapper action
        /// </summary>
        public void NextState()
        {
            if (m_State < GameState.AfterActionReview)
            {
                m_State++;
                if (m_State == GameState.GamePlay)
                {
                    // TODO
                }
            }
            else
            {
                m_State = GameState.OptionsMain;
            }
        }

        public void PointCamera(int count)
        {
            m_Engine.MoveCamera(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, count);
        }

        public void PitchCamera(int count)
        {
            m_Engine.MoveCamera(0.0f, 0.0f, 0.0f, count * 0.1f, 0.0f, 0.0f);
        }

        public void MoveCameraXP()
        {
            m_Engine.MoveCamera(0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
        }

        public void MoveCameraXM()
        {
            m_Engine.MoveCamera(-0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
        }

        public void MoveCameraY()
        {
            m_Engine.MoveCamera(0.0f, 0.5f, 0.0f, 0.0f, 0.0f, 0.0f);
        }

        public void MoveCameraZP()
        {
            m_Engine.MoveCamera(0.0f, 0.0f, 0.5f, 0.0f, 0.0f, 0.0f);
        }

        public void MoveCameraZM()
        {
            m_Engine.MoveCamera(0.0f, 0.0f, -0.5f, 0.0f, 0.0f, 0.0f);
        }

        protected override void RestoreDeviceObjects(object sender, EventArgs e)
        {
            // Restore the device objects for the meshes and fonts

            // set the transform matrices (view and world are updated per frame)
            Matrix matProj;
            float fAspect = device.PresentationParameters.BackBufferWidth / (float)device.PresentationParameters.BackBufferHeight;
            matProj = Matrix.PerspectiveFovLH((float)Math.PI / 4, fAspect, 1.0f, 100.0f);
            device.Transform.Projection = matProj;

            // Set up the default texture states
            device.TextureState[0].ColorOperation = TextureOperation.Modulate;
            device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
            device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;
            device.TextureState[0].AlphaOperation = TextureOperation.SelectArg1;
            device.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;
            device.SamplerState[0].MinFilter = TextureFilter.Linear;
            device.SamplerState[0].MagFilter = TextureFilter.Linear;
            device.SamplerState[0].MipFilter = TextureFilter.Linear;
            device.SamplerState[0].AddressU = TextureAddress.Clamp;
            device.SamplerState[0].AddressV = TextureAddress.Clamp;

            device.RenderState.DitherEnable = true;
        }

        /// <summary>
        /// Called when the app is exiting, or the device is being changed,
        /// this function deletes any device-dependent objects.
        /// </summary>
        protected override void DeleteDeviceObjects(object sender, EventArgs e)
        {
            m_Engine.Dispose();
            m_Console.Dispose();
        }

        public void LoadOptions()
        {
            try
            {
                System.Random rand = new System.Random();

                // loading of options will happen here
                m_Engine.SetTerrain(200, 200, @"..\..\Resources\heightmap.jpg", @"..\..\Resources\sand1.jpg", 10.0f, 0.45f);

                for (int i = 0; i<150; i++)
                {
                    float north = (float)(rand.NextDouble() * 1900.0);
                    float east = (float)(rand.NextDouble() * 1900.0);
                    BillBoard.Add(east, north, 0.0f, "cactus" + i, @"..\..\Resources\cactus.dds", 1.0f, 1.0f);
                }
                for (int i = 0; i < 150; i++)
                {
                    float north = (float)(rand.NextDouble() * 1900.0);
                    float east = (float)(rand.NextDouble() * 1900.0);
                    BillBoard.Add(east, north, 0.0f, "tree" + i, @"..\..\Resources\palmtree.dds", 6.5f, 10.0f);
                }
                GameEngine.Console.AddLine("all trees loaded");

                double j = 0.0f;
                double center_x = 1000.0;
                double center_z = 1000.0;
                double radius = 700.0;
                double width = 20.0;

                for (double i = 0.0; i < 360.0; i+=1.5)
                {
                    float north = (float)(center_z + Math.Cos(i / 180.0 * Math.PI) * radius);
                    float east = (float)(center_x + Math.Sin(i / 180.0 * Math.PI) * radius);
                    BillBoard.Add(east, north, 0.0f, "redpost" + (int)(i * 2), @"..\..\Resources\redpost.dds", 0.25f, 1.0f);
                    j += 5.0;
                    if (j > 360.0) j -= 360.0;
                }

                j = 0.0;
                for (double i = 0.5; i < 360.0; i += 1.5)
                {
                    float north = (float)(center_z + Math.Cos(i / 180.0 * Math.PI) * (radius+width));
                    float east = (float)(center_x + Math.Sin(i / 180.0 * Math.PI) * (radius + width));
                    BillBoard.Add(east, north, 0.0f, "redpost" + (int)(i * 2), @"..\..\Resources\bluepost.dds", 0.25f, 1.0f);
                    j += 5.0;
                    if (j > 360.0) j -= 360.0;
                }
            }
            catch (Exception e)
            {
                GameEngine.Console.AddLine("Exception");
                GameEngine.Console.AddLine(e.Message);
            }
        }
    }
}