-- Solution
solution "Pulsar4X"
	configurations { "Debug", "Release" }
	location "Pulsar4X"
	framework "4.0"

	
-- Find the required log4net library
function log4netlib()
	if (os.isdir("Pulsar4X/deps/log4net-1.2.11")) then -- Have the log4net binaries
		if (_OPTIONS.dotnet == "mono") then
			-- Assume Mono 2.0
			return "Pulsar4X/deps/log4net-1.2.11/bin/mono/2.0/release/log4net.dll"
		else
			-- .NET 4.0
			return "Pulsar4X/deps/log4net-1.2.11/bin/net/4.0/release/log4net.dll"
		end
	end
end

-- Find the required nunit library
function nunitlib()
	if(os.is("windows")) then
		if (os.isdir("Pulsar4X/deps/NUnit-2.6.1")) then -- Have the nunit binaries
			return "Pulsar4X/deps/NUnit-2.6.1/bin/nunit.framework.dll"
		end
	else
		return os.findlib("nunit.framework")
	end
end

-- Find the required sqlite library
function sqlitelib()
	if(os.is("windows")) then
		if (os.isdir("Pulsar4X/deps/Sqlite")) then -- Have the sqlite binaries
			--Assume .NET 4.0 for now, should work for mono
			return "Pulsar4X/deps/Sqlite/Windows-1.0.81.0/System.Data.SQLite.dll";
		end
	else
		return os.findlib("nunit.framework")
	end
end

-- Find the required dapper library
function dapperlib()
	if(os.is("windows")) then
		if (os.isdir("Pulsar4X/deps/Dapper/bin")) then -- Have the dapper binaries
			--Assume .NET 4.0 for now
			return "Pulsar4X/deps/Dapper/bin/Dapper.dll";
		end
	else
		return os.findlib("nunit.framework")
	end
end

-- Find the required OpenTK library
function opentklib()
	if(os.is("windows")) then
		if (os.isdir("Pulsar4X/deps/Dapper/bin")) then -- Have the OpenTK binaries
			--Assume .NET for now, should work for mono
			return "Pulsar4X/deps/OpenTK-1.0/bin/OpenTK.dll";
		end
	else
		return os.findlib("nunit.framework")
	end
end

-- Find the required OpenTK.GLControl library
function opentkglcontrollib()
	if(os.is("windows")) then
		if (os.isdir("Pulsar4X/deps/Dapper/bin")) then -- Have the OpenTK.GLControl binaries
			--Assume .NET for now, should work for mono
			return "Pulsar4X/deps/OpenTK-1.0/bin/OpenTK.GLControl.dll";
		end
	else
		return os.findlib("nunit.framework")
	end
end
	
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
			os.copyfile("Pulsar4X/deps/Sqlite/Windows-1.0.81.0/SQLite.Interop.dll", "Pulsar4X/Pulsar4X.Lib/bin/Debug/SQLite.Interop.dll")
			os.copyfile("Pulsar4X/deps/Sqlite/Windows-1.0.81.0/SQLite.Interop.pdb", "Pulsar4X/Pulsar4X.Lib/bin/Debug/SQLite.Interop.pdb")
		
		configuration { "windows", "Release" }
			os.copyfile("Pulsar4X/deps/Sqlite/Windows-1.0.81.0/SQLite.Interop.dll", "Pulsar4X/Pulsar4X.Lib/bin/Release/SQLite.Interop.dll")
			os.copyfile("Pulsar4X/deps/Sqlite/Windows-1.0.81.0/SQLite.Interop.pdb", "Pulsar4X/Pulsar4X.Lib/bin/Release/SQLite.Interop.pdb")
			
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
			sqlitelib(),
			dapperlib()
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
