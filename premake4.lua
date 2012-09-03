-- Solution
solution "Pulsar4X"
	configurations { "Debug", "Release" }
	location "Pulsar4X"
	framework "4.0"
	
	dofile "functions.lua"
	
	-- WinForms Project, main UI project
	project "Pulsar4X.WinForms"
		kind "WindowedApp"
		language "C#"
		location "Pulsar4X/Pulsar4X.WinForms"
		objdir "Pulsar4X/Pulsar4X.WinForms/obj"
		links { -- Add any needed references here
			"Pulsar4X.Lib",
			"System",
			"System.Data",
			"System.Windows.Forms",
			"System.Drawing",
			log4netlib(),
			opentklib(),
			opentkglcontrollib()
			}
		files { 
			"Pulsar4X/Pulsar4X.WinForms/**.cs",
			"Pulsar4X/Pulsar4X.WinForms/**.resx",
			"Pulsar4X/Pulsar4X.WinForms/**.config"
			}
		
		configuration "Debug"
			targetdir "Pulsar4X/Pulsar4X.WinForms/bin/Debug"
			defines { "DEBUG" }
			flags { "Symbols" }
			
		configuration "Release"
			targetdir "Pulsar4X/Pulsar4X.WinForms/bin/Release"
			flags { "Optimize" }
			
		configuration { "windows", "Debug" }
			os.copydir("Pulsar4X/Pulsar4X.WinForms/Resources", "Pulsar4X/Pulsar4X.WinForms/bin/Debug/Resources")
		
		configuration { "windows", "Release" }
			os.copydir("Pulsar4X/Pulsar4X.WinForms/Resources", "Pulsar4X/Pulsar4X.WinForms/bin/Release/Resources")
			
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
			log4netlib(),
			jsonlib()
			}
		files { 
			"Pulsar4X/Pulsar4X.Lib/**.cs",
			}
			
		configuration { "windows", "Release" }
			
		configuration "Debug"
			targetdir "Pulsar4X/Pulsar4X.Lib/bin/Debug"
			defines { "DEBUG" }
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
			defines { "DEBUG" }
			flags { "Symbols" }
			
		configuration "Release"
			targetdir "Pulsar4X/Pulsar4X.Tests/bin/Release"
			flags { "Optimize" }
			
-- defaultaction setup
function defaultaction(osName, actionName)
	if (actionName == nil) then
		_ACTION = _ACTION or osName
	end	   
	if os.is(osName) then
		_ACTION = _ACTION or actionName
	end
end

defaultaction "vs2010"

