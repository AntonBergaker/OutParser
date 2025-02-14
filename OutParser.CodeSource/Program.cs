﻿using OutParsing;
using System;

OutParser.Interceptors.ParseL1Implementation("69", "{x}", out int x);
Console.WriteLine(x);


OutParser.Interceptors.ParseL2Implementation("My name is Anton", "My name is {name}", out string name);
Console.WriteLine(name);


OutParser.Interceptors.ParseL3Implementation("I love vanilla and I cannot lie", "I love {flavor} and I cannot lie", out string flavor);
Console.WriteLine(flavor);