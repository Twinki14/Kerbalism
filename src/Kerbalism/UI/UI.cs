using System;
using Kerbalism.Database;

namespace Kerbalism
{
    public static class UI
    {
        public static void Init()
        {
            // create subsystems
            message = new Message();
            launcher = new Launcher();
            window = new Window((uint) Styles.ScaleWidthFloat(300), 0, 0);
        }

        public static void Sync()
        {
            window.Position(DB.ui.WinLeft, DB.ui.WinTop);
        }

        public static void Update(bool show_window)
        {
            // if gui should be shown
            if (show_window)
            {
                // as a special case, the first time the user enter
                // map-view/tracking-station we open the body info window
                if (MapView.MapIsEnabled && !DB.ui.MapViewed)
                {
                    Open(BodyInfo.Body_info);
                    DB.ui.MapViewed = true;
                }

                // update subsystems
                launcher.Update();
                window.Update();

                // remember main window position
                DB.ui.WinLeft = window.Left();
                DB.ui.WinTop = window.Top();
            }

            // re-enable camera mouse scrolling, as some of the on_gui functions can
            // disable it on mouse-hover, but can't re-enable it again consistently
            // (eg: you mouse-hover and then close the window with the cursor still inside it)
            // - we are ignoring user preference on mouse wheel
            GameSettings.AXIS_MOUSEWHEEL.primary.scale = 1.0f;
        }

        public static void On_gui(bool show_window)
        {
            // render subsystems
            message.On_gui();
            if (show_window)
            {
                launcher.On_gui();
                window.On_gui();
            }
        }

        public static void Open(Action<Panel> refresh)
        {
            window.Open(refresh);
        }

        static Message message;
        static Launcher launcher;
        public static Window window;
    }
} // KERBALISM
