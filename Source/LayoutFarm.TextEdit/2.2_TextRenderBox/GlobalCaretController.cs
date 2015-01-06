﻿//2014,2015 Apache2, WinterDev
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LayoutFarm.UI;
namespace LayoutFarm.Text
{

    static class GlobalCaretController
    {
        static bool enableCaretBlink = true;//default
        static TextEditRenderBox currentTextBox;
        static bool caretRegistered = false;
        static EventHandler<GraphicsTimerTaskEventArgs> tickHandler;
        static object caretBlinkTask = new object();
        static GraphicsTimerTask task;

        static GlobalCaretController()
        {
            tickHandler = new EventHandler<GraphicsTimerTaskEventArgs>(caret_TickHandler);
        }
        internal static void RegisterCaretBlink(RootGraphic root)
        {
            if (caretRegistered)
            {
                return;
            }
            caretRegistered = true;
            task = root.RequestGraphicsIntervalTask(
                caretBlinkTask,
                TaskIntervalPlan.CaretBlink,
                300,
                tickHandler);
        }
        static void caret_TickHandler(object sender, GraphicsTimerTaskEventArgs e)
        {
            if (currentTextBox != null)
            {
                currentTextBox.SwapCaretState();
                //force render ?
                currentTextBox.InvalidateGraphic();
                e.NeedUpdate = 1;
            }
            else
            {

            }

        }
        public static bool EnableCaretBlink
        {
            get { return enableCaretBlink; }
            set
            {
                enableCaretBlink = value;
            }
        }
        internal static TextEditRenderBox CurrentTextEditBox
        {
            get { return currentTextBox; }
            set
            {
                if (currentTextBox != value)//&& textEditBox != null)
                {
                    //make lost focus on current textbox
                    if (currentTextBox != null)
                    {
                        //stop caret on prev element
                        currentTextBox.SetCaretState(false);
                        var evlistener = currentTextBox.GetController() as IEventListener;

                        currentTextBox = null;

                        if (evlistener != null)
                        {
                            evlistener.ListenLostFocus(null);
                        }
                    }
                }
                currentTextBox = value;
            }
        }



    }

}