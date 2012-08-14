solution "Pulsar4X"
	configurations { "Debug", "Release" }
	location "Pulsar4X"
	
	project "Pulsar4X.WinForms"
		kind "WindowedApp"
		language "C#"
		location "Pulsar4X/Pulsar4X.WinForms"
		links { "System" }
		files { "Pulsar4X/Pulsar4X.WinForms/*.cs" }
		
		configuration "Debug"
			defines { "DEBUG" }
			flags { "Symbols" }
			
		configuration "Release"
			flags { "Optimize" }
			
	
	project "Pulsar4X.Lib"
		kind "SharedLib"
		language "C#"
		location "Pulsar4X/Pulsar4X.Lib"
		links { "System" }
		files { "Pulsar4X/Pulsar4X.Lib/*.cs" }
		
		configuration "Debug"
			defines { "DEBUG" }
			flags { "Symbols" }
			
		configuration "Release"
			flags { "Optimize" }