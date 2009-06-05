
import alive

def my_chat_callback(message, audible, type, sourcetype, fromname, id, ownerid, position):
    print "Chat message: " + message
    #print "Audible: " + audible
    #print "Type: " + type
    #print "Sourcetype: " + sourcetype
    print "Fromname: " + fromname
    print "Id: " + id.GetULong()
    print "Ownerid: " + ownerid.GetULong()
    #print "Position: " + position
    
alive.Initialize(my_chat_callback)

if alive.Login("Test", "User", "test"):
    print "Logged in!"
else:
	print "Failure"

raw_input("Press any key")

alive.Chat("Hello World")

raw_input("Press any key")


