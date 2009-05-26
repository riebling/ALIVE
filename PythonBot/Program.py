import clr
import sys
import time
clr.AddReferenceToFile("OpenMetaverse.dll")
import OpenMetaverse

client=OpenMetaverse.GridClient()
manager=OpenMetaverse.NetworkManager(client)

#params=client.Network.LoginParams('uri',1,'methodname','first','last','pass','start','channel','version','platform','mac','digest',[''],'id0')

def Network_OnConnected(sender):
    print "I'm connected to the simulator...\n" # or Console.WriteLine("I'm connected to the simulator...")
    client.Self.Chat("Hello Grid!", 0, OpenMetaverse.ChatType.Normal)
    print "Now I am going to logout of SL.. Goodbye!" # or print "Now I am going to logout of SL.. Goodbye!\n"
    client.Network.Logout()

client.Network.OnConnected += manager.ConnectedCallback(Network_OnConnected)
params=client.Network.DefaultLoginParams('Eric','Ruban','pincushion','boo','baz')
#params.URI="http://osmort.lti.cs.cmu.edu:9000"

print
print (params.URI)
print

if client.Network.Login(params):
#if client.Network.Login("Test", "User", "test", "MyBot", "osmort.lti.cs.cmu.edu:9000", "1.0" ):
 print "Success!\n"
else:
 print ("ERROR: " + client.Network.LoginMessage + "\n")

client.Network.Logout()

resp = raw_input("Press any key")