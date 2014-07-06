StarGen/starform/accrete has a long and varied history. Many people have altered it over time. The original program was written in Fortran about 30 years ago at Rand. About 15 years ago, in 1985, Martyn Fogg wrote a version of it and published his results. This lead Matt Burdick to create a version first in Pascal and then in C. This program was called "starform" that showed up on the net in about 1988. Several versions of starform have since been developed and distributed by Ian Burrell, Carl Burke and Chris Croughton AKA Keris.

Each of the authors has attempted to preserve the credits of those who came before, but it is often hard to know whose voice the different bits are written in. This is my attempt to continue the tradition of giving Credit where credit is due. I've missed things and made mistakes, no doubt.

JimB. aka Brons
Jim Burrows
Eldacur Technologies
brons@eldacur.com
http://www.eldacur.com/~brons/

        ================================================================
                Taken from Chris Croughton AKA Keris
        ================================================================

Starform (also known as Accrete) was created by Matt Burdick in 1988.  Since
then it has been hacked about by many people.  This version is based on the
original released 'starform' code, as far as I know, with my hacks to it to
make it do what I want.

My changes are:

  Convert it to use ANSI C function prototypes, and have a header file for
  each module containing the exported functions and variables.

  Make it more modular, so that modules are not using data from modules that
  call them (and hopefully not calling functions 'upward' either).
  
  Put the 'main' function as the only thing in starform.c, so that (hopefully)
  all the other modules can be called by other programs.

  Added stuff to list what materials are liquid on the planets' surface and
  what gasses might be in the atmosphere (if any).  It's guesswork to an
  extent (especially proportions), if you want to take it out look for
  function 'text_list_stuff' in module display.c and delete the calls to it.
  If you know more than I do about the amounts or properties (not hard!)
  please let me know any changes to make it better.

  I've also added some ad-hoc moon generation stuff, if you define 'MOON' when
  compiling (add '-DMOON' to the CFLAGS variable in the makefile) then you'll
  get moons generated as well.  It's not scientifically based, though, so you
  probably don't want to do that...

Please read the INSTALL file to find out how to build it.

Chris Croughton, 1999.04.19
mailto:chris@keris.demon.co.uk

        ================================================================
                Taken from Matt Burdick (via Kerris's ReadMe)
        ================================================================

This program is based on an article by Martyn Fogg in the Journal of the
British Interplanetary Society (JBIS) called 'Extrasolar Planetary Systems:
a Microcomputer Simulation'.  In it, he described how to generate various
sun-like solar systems randomly.  Since he did a good job of listing
references, I decided to implement it in Turbo Pascal on my PC.

Later, I translated it to C for portability, and the result is what you see
in front of you.  Because of my need to run this on an IBM-PC, there are two
makefiles included with the program: 'Makefile' (for unix systems), and 
'starform.mak' for MS-DOS systems.  To create the executable on a unix 
system, type in 'make' alone; type 'make starform.mak' if you are on 
an MS-DOS machine and using Microsoft C.

Thanks go to Sean Malloy (malloy@nprdc.arpa) for his help with the random
number routines and to Marty Shannon (mjs@mozart.att.com) for the
lisp-style output format, command-line flags, and various other pieces.

Enjoy, and if you find any glaring inconsistancies or interesting pieces to
add to the simulation, let me know and I'll include it in any other
distributions I send out.

Now for some references.  These are not the only good references on this 
subject; only the most interesting of many that were listed in Fogg's 
article in vol 38 of JBIS:

For a good description of the entire program:
	"Extra-Solar Planetary Systems: A Microcomputer Simulation"
	Martyn J. Fogg,  Journal of the British Interplanetary Society
	Vol 38, pp. 501 - 514, 1985

For the surface temperature/albedo iterative loop:
	"The Evolution of the Atmosphere of the Earth"
	Michael H. Hart, Icarus, Vol 33, pp. 23 - 39, 1978

For the determination of the radius of a terrestrial planet:
	"The Internal Constitution of the Planets"
	D. S. Kothari, Ph.D. , Mon. Not. Roy. Astr. Soc.
	Vol 96, pp. 833 - 843, 1936

For the planetary mass accretion algorithm:
	"Formation of Planetary Systems by Aggregation: A Computer Simulation"
	S. H. Dole, RAND paper no. P-4226, 1969

For the day length calculation:
	"Q in the Solar System"
	P. Goldreich and S. Soter, Icarus, Vol 5, pp. 375 - 389, 1966

----------------------------------------------------------------------
 I can be reached at the email address burdick%hpda@hplabs.hp.com
----------------------------------------------------------------------

        ================================================================
                From Carl Burke's credits of his version:
        ================================================================

Source Code 
I've contacted Matt Burdick (the author of starform) to find out exactly what restrictions he put on his code. (To be more precise, his brother saw this page and made the introductions.) As far as he and I are concerned, this software is free for your use as long as you don't sell it. I take that to mean that you can include this code in a commercial package (like a game) without fee, as long as that package isn't just a prettier version of this applet. There's a longer copyright notice in the source files, but this gets the gist across.
The source code is in this Unix tarred & gzipped file, or in this Windows/DOS zip archive.

------------------------------------------------------------------------
Acknowledgements 
Matt Burdick, the author of 'starform' (freely redistributable); much of the code (particularly planetary environments) was adapted from his code.
Andrew Folkins, the author of 'accretion' (public domain) for the Amiga; I used chunks of his code when creating my displays.
Ed Taychert of Irony Games, for the algorithm he uses to classify terrestrial planets in his tabular CGI implementation of 'starform'.
Paul Schlyter, who provided information about computing planetary positions.
Planetary images courtesy Jet Propulsion Laboratory. Copyright (c) California Institute of Technology, Pasadena, CA. All rights reserved. 

------------------------------------------------------------------------
Bibliography 
These sources are the ones quoted by Burdick in the code. A good web search (or more old-fashioned literature search) will identify literally hundreds of papers regarding the formation of the solar system and the evolution of proplyds (protoplanetary discs). Most of these sources can be difficult for a layman like myself to locate, but those journals are the best place to get up to speed on current theories of formation.

"Extra-Solar Planetary Systems: A Microcomputer Simulation", Martyn J. Fogg, Journal of the British Interplanetary Society Vol 38, pp. 501 - 514, 1985
"The Evolution of the Atmosphere of the Earth", Michael H. Hart, Icarus, Vol 33, pp. 23 - 39, 1978

"The Internal Constitution of the Planets", D. S. Kothari, Ph.D. , Mon. Not. Roy. Astr. Soc. Vol 96, pp. 833 - 843, 1936

"Formation of Planetary Systems by Aggregation: A Computer Simulation", S. H. Dole, RAND paper no. P-4226, 1969

"Habitable Planets for Man", S. H. Dole, Blaisdell Publishing Company, NY, 1964.

"Q in the Solar System", P. Goldreich and S. Soter, Icarus, Vol 5, pp. 375 - 389, 1966

        ================================================================
                From Ian Burrell's web page.
        ================================================================

History

Long ago, before I was born, there was published an article by Dole about simulating the creation of planetary systems by accretion. This simple model was later analyzed in another paper by Carl Sagan. A later article published Fortran code for implementing the original algorithm and for simulating the creation of a planetary system. 

As far as I know, the first widespread implementation was done by Matt Burdick. He wrote a Turbo Pascal version, a C version, and put together a package called starform. All of these implementations include environmental code to create and calculate temperature, atmosphere, and other environmental parameters beyond those of the Dole accretion model. 

The accrete program got a wide distribution from the USML mailing list in 1988. That mailing list's lofty goal was to explore the simulation of the universe, concentrating on the creation of fictional planetary systems, random terrain generation, and modeling human societies. It eventually died out due to lack of interest and a broadening of topic. 

Although this model is good enough for creating fictional star systems, the current theory for the creation of the solar system has moved on. Current scientific models are much more complicated and require significant computation time. The discovery of extrasolar planets has raised significant questions about the formation of planets. 

References

*	Dole, S. "Computer Simulation of the Formation of Planetary Systems". Icarus, vol 13, pp 494-508, 1970. 
*	Isaacman, R. & Sagan, C. "Computer Simulation of Planetary Accretion Dynamics: Sensitivity to Initial Conditions". Icarus, vol 31, p 510, 1977. 
*	Fogg, Martyn J. "Extra-Solar Planetary Systems: A Microcomputer Simulation". Journal of the British Interplanetary Society, vol 38, p 501-514, 1985. 

------------------------------------------------------------------------
Ian Burrell / iburrell@znark.com

        ================================================================
                                  Done
        ================================================================
