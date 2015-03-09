dofile "functions.lua"

-- Solution
solution "Pulsar4X"
	configurations { "Debug", "Release" }
	location "Pulsar4X"
	framework "4.0"

	-- WinForms Project, main UI project
	project "Pulsar4X.UI"
		kind "WindowedApp"
		language "C#"
		location "Pulsar4X/Pulsar4X.UI"
		objdir "Pulsar4X/Pulsar4X.UI/obj"
		links { -- Add any needed references here
			"Pulsar4X.Lib",
			"Pulsar4X.ECSLib",
			"System",
			"System.Data",
			"System.Windows.Forms",
			"System.Drawing",
			"System.Xml",
			log4netlib(),
			jsonlib(),
			opentklib(),
			opentkglcontrollib(),
			dockPanelSuiteLib()
			}
		files { 
			"Pulsar4X/Pulsar4X.UI/**.cs",
			"Pulsar4X/Pulsar4X.UI/**.resx",
			"Pulsar4X/Pulsar4X.UI/**.config",
			"Pulsar4X/Pulsar4X.UI/Resources/**"
			}
		excludes {
			"Pulsar4X/Pulsar4X.UI/bin/**",
			"Pulsar4X/Pulsar4X.UI/obj/**"
			}
		
		configuration "Resources/**"
			buildaction "copy"
		
		configuration "Debug"
			targetdir "Pulsar4X/Pulsar4X.UI/bin/Debug"
			defines { "DEBUG", "OPENGL", "SPLASHSCREEN", "LOG4NET_ENABLED" }
			flags { "Symbols" }
			
		configuration "Release"
			targetdir "Pulsar4X/Pulsar4X.UI/bin/Release"
			defines { "OPENGL", "SPLASHSCREEN", "LOG4NET_ENABLED" }
			flags { "Optimize" }

	-- Lib Project, contains game files
	project "Pulsar4X.Lib"
		kind "SharedLib"
		language "C#"
		location "Pulsar4X/Pulsar4X.Lib"
		objdir "Pulsar4X/Pulsar4X.Lib/obj"
		links { -- Add any needed references here
			"System",
			"System.Data",
			"System.Xml",
			"System.Drawing",
			log4netlib(),
			jsonlib()
			}
		files { 
			"Pulsar4X/Pulsar4X.Lib/**.cs",
			"Pulsar4X/Pulsar4X.Lib/Data/**"
			}
			
		configuration { "windows", "Release" }
			
		configuration "Data/**"
			buildaction "copy"
			
		configuration "Debug"
			targetdir "Pulsar4X/Pulsar4X.Lib/bin/Debug"
			defines { "DEBUG", "LOG4NET_ENABLED" }
			flags { "Symbols" }
			
		configuration "Release"
			targetdir "Pulsar4X/Pulsar4X.Lib/bin/Release"
			flags { "Optimize" }

	-- Unit Test Project, contains unit tests
	project "Pulsar4X.Tests"
		kind "SharedLib"
		language "C#"
		location "Pulsar4X/Pulsar4X.Tests"
		objdir "Pulsar4X/Pulsar4X.Tests/obj"
		links { -- Add any needed references here
			"Pulsar4X.Lib",
			"Pulsar4X.ECSLib",
			"System",
			"System.Data",
			"System.Xml",
			nunitlib()
			}
		files { 
			"Pulsar4X/Pulsar4X.Tests/**.cs",
			}
		
		configuration "Debug"
			targetdir "Pulsar4X/Pulsar4X.Tests/bin/Debug"
			defines { "DEBUG", "LOG4NET_ENABLED" }
			flags { "Symbols" }
			
		configuration "Release"
			targetdir "Pulsar4X/Pulsar4X.Tests/bin/Release"
                        defines { "LOG4NET_ENABLED" }
			flags { "Optimize" }
			
	project "Pulsar4X.ECSLib"
		kind "SharedLib"
		language "C#"
		location "Pulsar4X/Pulsar4X.ECSLib"
		objdir "Pulsar4X/Pulsar4X.ECSLib/obj"
		links { -- Add any needed references here
			"System",
			"System.Data",
			"System.Xml",
			"System.Drawing",
			}
		files { 
			"Pulsar4X/Pulsar4X.ECSLib/**.cs",
			}
			
		configuration { "windows", "Release" }
			
		configuration "Debug"
			targetdir "Pulsar4X/Pulsar4X.ECSLib/bin/Debug"
			flags { "Symbols" }
			
		configuration "Release"
			targetdir "Pulsar4X/Pulsar4X.ECSLib/bin/Release"
			flags { "Optimize" }

defaultaction "vs2010"

debugoutput()
