﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Gallio.Common.Markup;
using Gallio.Framework;
using Gallio.Framework.Pattern;
using Gallio.Model;
using MbUnit.Framework;
using OpenQA.Selenium;
using ProtoTest.Golem.Proxy;
using ProtoTest.Golem.WebDriver;

namespace ProtoTest.Golem.Core
{
    public abstract class TestBase
    {
        #region Events

#pragma warning disable 67
        public static event ActionEvent BeforeTestEvent;
        public static event ActionEvent AfterTestEvent;
        public static event ActionEvent PageObjectActionEvent;
        public static event ActionEvent BeforeCommandEvent;
        public static event ActionEvent AfterCommandEvent;
        public static event ActionEvent BeforeSuiteEvent;
        public static event ActionEvent AfterSuiteEvent;
        public static event ActionEvent GenericEvent;
#pragma warning restore 67
        private void WriteActionToLog(string name, EventArgs e)
        {
            if (Config.Settings.reportSettings.commandLogging)
                Common.Log("(" + DateTime.Now.ToString("HH:mm:ss::ffff") + ") : " + name);
        }

        private void AddAction(string name, EventArgs e)
        {
            testData.actions.addAction(name);
        }

        public delegate void ActionEvent(string name, EventArgs e);

        public static void LogEvent(string name)
        {
            GenericEvent(name, null);
        }

        public class GolemEventArgs : EventArgs
        {
            public IWebDriver driver;
            public IWebElement element;
            public string name;
            public DateTime time;
        }

        #endregion

        protected static Object locker = new object();
        public static BrowserMobProxy proxy;

        [SetUp]
        public void SetUp()
        {
            LogEvent(Common.GetCurrentTestName() + " started");
            StartNewProxy();
        }

        [TearDown]
        public void TearDown()
        {
            TestContext.CurrentContext.AutoExecute(TriggerEvent.TestPassed, () =>
            {
                try
                {
                }
                catch (Exception e)
                {
                }
            });
            LogEvent(Common.GetCurrentTestName() + " " + Common.GetTestOutcome().DisplayName);
            GetHarFile();
            QuitProxy();
            AssertNoVerificationErrors();
            DeleteTestData();
        }


        [FixtureSetUp]
        public void SuiteSetUp()
        {
            SetupEvents();
            testDataCollection = new Dictionary<string, TestDataContainer>();
            SetTestExecutionSettings();
            StartProxyServer();
        }


        [FixtureTearDown]
        public void SuiteTearDown()
        {
            QuitProxyServer();
            RemoveEvents();
            Config.Settings = new ConfigSettings();
        }

        private void DeleteTestData()
        {
            string testName = Common.GetCurrentTestName();
            if (!testDataCollection.ContainsKey(testName))
            {
                testDataCollection.Remove(testName);
            }
        }

        private static IDictionary<string, TestDataContainer> _testDataCollection;

        public static IDictionary<string, TestDataContainer> testDataCollection
        {
            get { return _testDataCollection ?? new Dictionary<string, TestDataContainer>(); }
            set { _testDataCollection = value; }
        }

        public static TestDataContainer testData
        {
            get
            {
                string name = Common.GetCurrentTestName();
                if (!testDataCollection.ContainsKey(name))
                {
                    lock (locker)
                    {
                        var container = new TestDataContainer(name);
                        testDataCollection.Add(name, container);
                        return container;
                    }
                }
                return testDataCollection[name];
            }
        }


        public static void AddVerificationError(string errorText)
        {
            LogEvent("--> VerificationError Found: " + errorText);
            testData.VerificationErrors.Add(new VerificationError(errorText));
            TestContext.CurrentContext.IncrementAssertCount();
        }

        public static void AddVerificationError(string errorText, Image image)
        {
            LogEvent("--> VerificationError Found: " + errorText);
            testData.VerificationErrors.Add(new VerificationError(errorText, image));
            TestContext.CurrentContext.IncrementAssertCount();
        }

        private void AssertNoVerificationErrors()
        {
            if (testData.VerificationErrors.Count == 0)
                return;
            int i = 1;
            TestLog.BeginMarker(Marker.AssertionFailure);
            foreach (VerificationError error in testData.VerificationErrors)
            {
                TestLog.Failures.BeginSection("ElementVerification Error " + i);
                TestLog.Failures.WriteLine(error.errorText);
                if (error.screenshot != null)
                    TestLog.Failures.EmbedImage(null, error.screenshot);
                TestLog.Failures.End();
                i++;
            }
            TestLog.End();
            Assert.TerminateSilently(TestOutcome.Failed);
        }


        public void SetTestExecutionSettings()
        {
            TestAssemblyExecutionParameters.DegreeOfParallelism = Config.Settings.runTimeSettings.DegreeOfParallelism;
            TestAssemblyExecutionParameters.DefaultTestCaseTimeout =
                TimeSpan.FromMinutes(Config.Settings.runTimeSettings.TestTimeoutMin);
        }

        private void SetupEvents()
        {
            PageObjectActionEvent += AddAction;
            BeforeTestEvent += WriteActionToLog;
            AfterTestEvent += WriteActionToLog;
            GenericEvent += WriteActionToLog;
        }

        private void RemoveEvents()
        {
            PageObjectActionEvent -= AddAction;
            BeforeTestEvent -= WriteActionToLog;
            AfterTestEvent -= WriteActionToLog;
            GenericEvent -= WriteActionToLog;
        }


        private void GetHarFile()
        {
            try
            {
                if (Config.Settings.httpProxy.useProxy)
                {
                    string name = Common.GetShortTestName(80);
                    proxy.SaveHarToFile();
                    TestLog.Attach(new BinaryAttachment("HTTP_Traffic_" + name + ".har",
                        "application/json", File.ReadAllBytes(proxy.GetHarFilePath())));
                    proxy.CreateHar();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error caught getting BMP Har File : " + e.Message);
            }
        }

        private void StartNewProxy()
        {
            try
            {
                if (Config.Settings.httpProxy.startProxy)
                {
                    proxy.CreateProxy();
                    proxy.CreateHar();
                }
            }
            catch (Exception e)
            {
                proxy.CreateProxy();
                proxy.CreateHar();
            }
        }

        private void StartProxyServer()
        {
            try
            {
                if (Config.Settings.httpProxy.startProxy)
                {
                    proxy = new BrowserMobProxy();
                    proxy.KillOldProxy();
                    proxy.StartServer();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error caught starting BMP Proxy Server : " + e.Message);
            }
        }

        private void QuitProxyServer()
        {
            try
            {
                if (Config.Settings.httpProxy.startProxy)
                {
                    proxy.QuitServer();
                }
            }
            catch (Exception e)
            {
            }
        }

        private void QuitProxy()
        {
            try
            {
                if (Config.Settings.httpProxy.startProxy)
                {
                    proxy.QuitProxy();
                }
            }
            catch (Exception e)
            {
            }
        }

        public static string GetCurrentClassName()
        {
            var stackTrace = new StackTrace(); // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames(); // get method calls (frames)

            foreach (StackFrame stackFrame in stackFrames)
            {
                // trace += stackFrame.GetMethod().ReflectedType.FullName;
                if (stackFrame.GetMethod().ReflectedType.FullName.Contains("PageObject"))
                {
                    string name = stackFrame.GetMethod().ReflectedType.Name;
                    return name;
                }
            }
            // DiagnosticLog.WriteLine(stackTrace.ToString());
            return "";
        }

        public static string GetCurrentMethodName()
        {
            var stackTrace = new StackTrace(); // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames(); // get method calls (frames)

            // write call stack method names
            foreach (StackFrame stackFrame in stackFrames)
            {
                if (stackFrame.GetMethod().ReflectedType.BaseType == typeof (BasePageObject))
                    return stackFrame.GetMethod().Name;
            }
            return "";
        }

        public static string GetCurrentClassAndMethodName()
        {
            var stackTrace = new StackTrace(); // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames(); // get method calls (frames)
            // string trace = "";
            foreach (StackFrame stackFrame in stackFrames)
            {
                // trace += stackFrame.GetMethod().ReflectedType.FullName;
                if ((stackFrame.GetMethod().ReflectedType.FullName.Contains("PageObject")) &&
                    (!stackFrame.GetMethod().IsConstructor))
                {
                    string name = stackFrame.GetMethod().ReflectedType.Name + "." + stackFrame.GetMethod().Name;
                    return name;
                }
            }
            // DiagnosticLog.WriteLine(stackTrace.ToString());
            return "";
        }
    }
}