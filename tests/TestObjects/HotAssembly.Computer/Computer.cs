﻿//-----------------------------------------------------------------------
//Copyright 2015-2016 Roman Tumaykin
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//-----------------------------------------------------------------------
using System;
using Newtonsoft.Json;

namespace HotAssembly.Computer
{
    [Serializable]
    public class Computer : IComputer
    {
        public string GetAppDomain()
        {
            return $"{JsonConvert.SerializeObject(GetType().Assembly.Location)}-//{GetType()}!!!//FileName:{SomeOtherProcess.Proc1.GetStuff()}";
        }
    }

    public class Computer1 : IComputer
    {
        public string GetAppDomain()
        {
            return $"{JsonConvert.SerializeObject(GetType().Assembly.Location)}-//{GetType()}!!!//FileName:{SomeOtherProcess.Proc1.GetStuff()}";
        }
    }
}
