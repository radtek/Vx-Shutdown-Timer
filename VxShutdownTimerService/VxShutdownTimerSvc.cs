﻿using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;
using CoreLib;
using CoreLib.IO;
using CoreLib.Models;
using System.IO;
using ShutdownLib;
namespace VxShutdownTimerService
{
    public partial class VxShutdownTimerSvc : ServiceBase
    {
        private Timer _timer;
        private bool _isTimerRunning;
        private List<ShutdownModel> _list;
        public VxShutdownTimerSvc()
        {
            InitializeComponent();
            LoadComponents();

        }

        private void LogError(string errorMessage)
        {
            try
            {
                if(File.Exists(Global.GetLogFileLocation()))
                {

                    File.AppendAllText(Global.GetLogFileLocation(), errorMessage + "\r\n\r\n");
                }
            }
            catch { }
        }

        private void LoadComponents()
        {
          
            _timer = new Timer
            {
                Interval = 1000
            };
            _timer.Elapsed += TimerElapsed;
            _list = new List<ShutdownModel>();
        }
        protected override void OnStart(string[] args)
        {
            StartContinueService();
        }
        protected override void OnContinue()
        {
            StartContinueService();
        }
        protected override void OnPause()
        {
            PauseStopService();
        }
        protected override void OnStop()
        {
            PauseStopService();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            Check();
        }
        private void Invoker(ShutdownType type)
        {
            try
            {
                switch (type)
                {
                    case ShutdownType.Shutdown:
                        ShutdownInvoker.InvokeShutdown();
                        break;
                    case ShutdownType.Hibernate:
                        ShutdownInvoker.SetSuspendState(true, true, true);
                        break;
                    case ShutdownType.Sleep:
                        ShutdownInvoker.SetSuspendState(false, true, true);
                        break;
                    case ShutdownType.LogOff:
                        ShutdownInvoker.InvokeLogOffSvc();
                        break;
                    case ShutdownType.Restart:
                        ShutdownInvoker.InvokeRestart();
                        break;
                }
            }
            catch(Exception ex)
            {
                LogError(ex.Message);
            }
        }
        private void Check()
        {
            try
            {
                foreach(ShutdownModel item in _list)
                {
                    DateTime now;
                    switch (item.Repetition)
                    {
                        case Repetition.None:
                            now = DateTime.Now;
                            //for faster match!
                            if ((now.TimeOfDay.Seconds == item.DateTime.TimeOfDay.Seconds) &&
                                (now.TimeOfDay.Minutes == item.DateTime.TimeOfDay.Minutes) &&
                                (now.TimeOfDay.Hours == item.DateTime.TimeOfDay.Hours) && 
                                (now.Date == item.DateTime.Date))
                            {
                                Task.Run(() => Invoker(item.ShutdownType));
                            }
                            break;
                        case Repetition.Daily:
                             now = DateTime.Now;
                            //for faster match!
                            if ((now.TimeOfDay.Seconds == item.DateTime.TimeOfDay.Seconds) &&
                                (now.TimeOfDay.Minutes == item.DateTime.TimeOfDay.Minutes) &&
                                (now.TimeOfDay.Hours == item.DateTime.TimeOfDay.Hours))
                            {
                                Task.Run(() => Invoker(item.ShutdownType));
                            }
                            break;
                        case Repetition.Weekly:
                            now = DateTime.Now;
                            //for faster match!
                            if ((now.TimeOfDay.Seconds == item.DateTime.TimeOfDay.Seconds) &&
                                (now.TimeOfDay.Minutes == item.DateTime.TimeOfDay.Minutes) &&
                                (now.TimeOfDay.Hours == item.DateTime.TimeOfDay.Hours) &&
                                (now.Day == item.DateTime.Day))
                            {
                                Task.Run(() => Invoker(item.ShutdownType));
                            }
                                break;
                        case Repetition.Monthly:
                            now = DateTime.Now;
                            //for faster match!
                            if ((now.TimeOfDay.Seconds == item.DateTime.TimeOfDay.Seconds) &&
                                (now.TimeOfDay.Minutes == item.DateTime.TimeOfDay.Minutes) &&
                                (now.TimeOfDay.Hours == item.DateTime.TimeOfDay.Hours) &&
                                (now.Day == item.DateTime.Day) &&
                                (now.Month == item.DateTime.Month))
                            {
                                Task.Run(() => Invoker(item.ShutdownType));
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"---> {DateTime.Now} {ex.Message}");
            }
        }
        private void StartContinueService()
        {
            try
            {
                string filename = Global.GetDataFileLocation();
                if(File.Exists(filename))
                {
                    var result = IOSerializeDeserialize.Deserialize();
                    if(result.Status.Success)
                    {

                        _list = result.Data;
                        if(_list.Count>0)
                        {
                            _isTimerRunning = true;
                            _timer.Start();
                        }
                        else
                        {
                            _isTimerRunning = false;
                        }
                    }
                    else
                    {
                        _isTimerRunning = false;
                    }
                }
                else
                {
                    LogError($"---> {DateTime.Now} Error: Data file is not found");
                }
            }
            catch(Exception ex)
            {
                LogError($"---> {DateTime.Now} {ex.Message}");
            }
        }
        private void PauseStopService()
        {
            try
            {
                if(_isTimerRunning)
                {
                    _timer.Stop();
                }
            }
            catch (Exception ex)
            {
                LogError($"---> {DateTime.Now} {ex.Message}");
            }
        }
        
    }
}
