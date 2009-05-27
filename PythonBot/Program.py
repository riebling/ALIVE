import clr
import sys
import time

# libraries have to be loaded dynamically at run time
clr.AddReferenceToFile("OpenMetaverse.dll")
import OpenMetaverse

# OpenMetaverse client instance

client=OpenMetaverse.GridClient()
manager=OpenMetaverse.NetworkManager(client)

# Callbacks

def Network_OnConnected(sender):
    print "I'm connected to the simulator...\n"

# Register connected callback

client.Network.OnConnected += manager.ConnectedCallback(Network_OnConnected)

# Log in
client.Settings.LOGIN_SERVER="http://osmort.lti.cs.cmu.edu:9000";
params=client.Network.DefaultLoginParams('Test','User','test','ALIVE','alive')

if client.Network.Login(params):
 print "Success!\n"
else:
 print ("ERROR: " + client.Network.LoginMessage + "\n")
 client.Network.Logout()
 
respo = raw_input("Press any key")

client.Self.Chat("Hello Grid!", 0, OpenMetaverse.ChatType.Normal)
    
#resp = raw_input("Press any key")
print "Now I am going to logout of SL.. Goodbye!"

client.Network.Logout()

resp = raw_input("Press any key")