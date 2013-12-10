﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MbUnit.Framework;
using ProtoTest.Golem.Core;
using ProtoTest.Golem.WebDriver;

namespace Golem.Tests
{
    public class ConfigTests : TestBase
    {

        [Test]
        public void TestConfigSettingsAreModifiyable()
        {
            Config.Settings.runTimeSettings.ElementTimeoutSec = 1234;
            Assert.AreEqual(Config.Settings.runTimeSettings.ElementTimeoutSec,1234);
        }
        [Test]
        public void TestConfigFileDefaults()
        {
            var value = Config.GetConfigValue("Testsdflksjdflsdkj", "-1");
            Assert.AreEqual(value,"-1");
        }

    }
}
