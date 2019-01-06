# ALIVE
ALIVE OpenMetaverse API for SmartBots 
ALIVE Project
Introduction

We envision ALIVE as a set of tools that enables students to construct virtual robots capable of carrying out relatively simple assignments given to them in a natural language. Here is an example of such an assignment:

    There are trees behind the big house on the right.
    Find the small red ball near the tall tree.
    Bring it to the small house on the left. 

Game

Game: lists the game's rules/mechanics, technical components, and known issues.
ALIVE API

 HTML Documentation Tree
SIMULATOR

Simulator
WEB INTERFACES
wifi - new account creation

 http://ohio.lti.cs.cmu.edu:9000/wifi

This supports avatar creation, forgotten password recovery, and rudimentary inventory management via a web page. Default avatar types include Male, Female, Dog, Monkey, and Hippo. Unfortunately the current version of the server software has a database bug that prevents this from working.
Modlos - server / avatar stats

 http://ohio.lti.cs.cmu.edu/moodle/blocks/modlos

Currently offline, sorry.
STUDENT WIKI

This is where students can download the minimal "skeleton" Visual Studio C# project that enables them to build a simple GUI and connect to the virtual world server at CMU

    GettingStarted

Second Life

 http://secondlife.com/
OpenMetaverse

 http://openmetaverse.org/
OpenSim

 http://opensimulator.org/

Second Life is the 3D virtual world format we are working with. We will be using the OpenMetaverse C# open source libraries to access an OpenSim virtual world simulator hosted at CMU. You can access the CMU server by downloading the SecondLife viewer program here:

     http://www.phoenixviewer.com/

By default it will log you into SecondLife proper, where you would have to choose an avatar, a first and last name, and a password. But we will be using avatars already provided on an OpenSim simulator ("sim") server running on ohio.lti.cs.cmu.edu:9000 (IP address 128.2.208.30), using names like "Test User", "Test User2", "Test User3" etc. all with the password "test". Other logins include "Dog Larry", "Master Larry", "Dog Curly", "Master Curly" with the password "alive"

Use the "Grids" feature of the viewer to create and use an entry for the ALIVE grid using the above IP address.
Tools
Visual C#

We like to use the Microsoft Visual C# Express IDE, which is a free download:

 Visual C# Express

It is also possible to develop on Ubuntu linux (or others with MONO support) using monodevelop and nant.
OpenMetaverse Library

 OpenMetaverse

A copy of this is included in the ALIVE Subversion project, and so needn't be downloaded or installed separately.
Code Repository

We're storing code in a Subversion repository. You'll need an SVN client in order to access it. The repository can be also browsed using the "browse source" link at the top of this page. Windows users will need to download a client such as  Tortoise SVN

(Email er1k@cs.cmu.edu for an account/password for access.)

To get the MyBot project, ALIVE API, and OpenMetaverse code from the SVN library on linux:

svn co svn://mu.lti.cs.cmu.edu/ALIVE

on Windows (using TortoiseSVN):

    open a window to ...\My Documents\Visual Studio 2008\Projects
    right click and chose SVN Checkout...
    where it says "URL of Repository" enter svn://mu.lti.cs.cmu.edu/ALIVE
    click the "Yes" button to the pop-up asking if you'd like to create the folder first 

MyBot and ALIVE API

A GUI interface that provides several capabilities:

    Log in a named 'bot' avatar to Second Life or a local OpenSim
    Move the avatar by so many meters in X and Y direction
    View nearby chat
    view a list of nearby avatars
    Chat
    View all objects within a specified radius
        Object name
        Object description
        Object location
        Object size 
    Generally exercise all the ALIVE API methods 

Building and packaging the API

Subversion Repository: svn://mu.lti.cs.cmu.edu/ALIVE

The MyBot code is replicated in two projects, it exists with the ALIVE API code for debugging, and it is also delivered to students as part of the ALIVE-skeleton. It's not clear how to structure multiple Visual C# Studio projects to use the same code, so the duplication of code is a necessary evil.

In the MyBot solution (which includes the ALIVE project, and API.cs) once built, the output resides in ...\bin\Debug\ALIVE.dll. This file, as well as two other files ALIVE.pdb (debugging symbols) and ALIVE.XML (documentation) should be copied into the ALIVE-skeleton project's ALIVE-skeleton\bin\Debug\ for delivery to students. Once the three files are copied from ALIVE\bin\Debug you'll want to create a new zip file containing the API DLLs as part of the ALIVE-skeleton ZIP file. I've made a script to simplify this process - in order to prevent the ZIP file from containing the Subversion .svn folders. If you then check-in the ZIP file to Subversion, it will appear by magic in the links from the ALIVE Student wiki.

mkzip.bat

zip -r ALIVE-skeleton.zip ALIVE-skeleton -x \*.svn\*

TRAC Info

    TracGuide -- Built-in Documentation
     The Trac project -- Trac Open Source Project
     Trac FAQ -- Frequently Asked Questions
    TracSupport -- Trac Support 

For a complete list of local wiki pages, see TitleIndex.
