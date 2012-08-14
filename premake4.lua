-- Solution
solution "Pulsar4X"
	configurations { "Debug", "Release" }
	location "Pulsar4X"
	
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
			"System.Drawing"
			}
		files { "Pulsar4X/Pulsar4X.WinForms/**.cs" }
		
		configuration "Debug"
			targetdir "Pulsar4X/Pulsar4X.WinForms/bin/Debug"
			defines { "DEBUG" }
			flags { "Symbols" }
			
		configuration "Release"
		targetdir "Pulsar4X/Pulsar4X.WinForms/bin/Release"
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
			"System.Xml"
			}
		files { 
			"Pulsar4X/Pulsar4X.Lib/**.cs",
			}
		
		configuration "Debug"
			targetdir "Pulsar4X/Pulsar4X.Lib/bin/Debug"
			defines { "DEBUG" }
			flags { "Symbols" }
			
		configuration "Release"
			targetdir "Pulsar4X/Pulsar4X.Lib/bin/Release"
			flags { "Optimize" }
			
			
			
function defaultaction(osName, actionName)
   if (actionName == nil) then
     _ACTION = _ACTION or osName
   end	   
   if os.is(osName) then
      _ACTION = _ACTION or actionName
   end
end

defaultaction "vs2010"