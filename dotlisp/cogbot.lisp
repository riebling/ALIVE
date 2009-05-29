; Note: to find event generators in C# code look for
;   enqueueLispTask("(on

;----------------------------------
; Login and Network events
;----------------------------------
 (def (on-login-fail  login description)
  (block
    (thisClient.msgClient (@"(on-login-fail ({0}) ({1}))" (str login)(str description)) )
    )
 )

(def (on-login-success  login description)
  (block
    (thisClient.msgClient (@"(on-login-success ({0}) ({1}))" (str login)(str description)) )
    )
 )

 (def (on-network-disconnected reason message)
  (block
    (thisClient.msgClient (@"(on-network-disconnected ({0}) ({1}))" (str reason)(str message)) )
    )
 )
 
 ;--------------------------------------
 ; Here the bot is officially connected (I think), so you could 
 ; have it perform a initial inworld tasks like wearing some clothes
 ;-------------------------------------
 (def (on-network-connected reason message)
  (block
    (thisClient.msgClient (@"(on-network-connected )" ) )
    (thisClient.ExecuteCommand “say Hello World”)   
    (thisClient.ExecuteCommand “use HMG to wear”)
    )
 )

(def (on-simulator-connected simulator)
  (block
    (thisClient.msgClient (@"(on-simulator-connected ({0}) )" (str simulator)) )
    )
 )
 
;----------------------------------
; Avatars and objects
;----------------------------------
(def (on-new-avatar  avatar-name avatar-uuid)
  (block
    (thisClient.msgClient (@"(on-new-avatar ({0}) ({1}))" (str avatar-name)(str avatar-uuid)) )
    )
 )

(def (on-new-prim  prim-name prim-uuid prim-description)
  (block
    (thisClient.msgClient (@"(on-new-prim ({0}) ({1}) ({2}))" (str prim-name)(str prim-uuid)(str prim-description)) )
    )
 )
(def (on-new-foliage  foliage-name foliage-uuid foliage-description)
  (block
    (thisClient.msgClient (@"(on-new-prim ({0}) ({1}) ({2}))" (str foliage-name)(str foliage-uuid)(str foliage-description)) )
    )
 )


;-----------------------------
; In World Events
;-----------------------------
;  (on-chat agent message) -> "(heard (agent) message)";
(def (on-chat agent message)
  (block
    (thisClient.msgClient (@"(heard ({0}) '{1}')" (str agent)(str message)) )
    )
 )
 
 ;  (on-instantmessage agent message) -> "(heard (agent) message)";
(def (on-instantmessage agent message)
  (block
    (thisClient.msgClient (@"(heard-in-im ({0}) '{1}')" (str agent)(str message)) )
    )
 )

 
;  (on-meanCollision perp victim) -> "(collision (perp) (victim) )";
(def (on-meanCollision perp victim)
  (block
    (thisClient.msgClient (@"(collision ({0}) ({1}))" (str perp)(str victim)) )
    )
 )
 
 ;----------------------------------
; Looking and Pointing events
; on-self-look-target occurs when someone looks or mouses at the Cogbot avatar
;----------------------------------
 (def (on-self-look-target  source description)
  (block
    (thisClient.msgClient (@"(on-self-look-target ({0}) ({1}))" (str source)(str description)) )
    )
 )
 
 (def (on-self-point-target  source description)
  (block
    (thisClient.msgClient (@"(on-self-point-target ({0}) ({1}))" (str source)(str description)) )
    )
 )

 (def (on-avatar-point  source  dest description)
  (block
    (thisClient.msgClient (@"(on-avatar-point ({0}) ({1}))" (str source)(str description)) )
    )
 )

 (def (on-avatar-look  source  dest description)
  (block
    (thisClient.msgClient (@"(on-avatar-look ({0}) ({1}))" (str source)(str description)) )
    )
 )
 
;---------------------------------
; avatar descriptions
;---------------------------------
;  (on-avatar-dist agent dist) -> "(distance (agent) distance)";
(def (on-avatar-dist agent dist)
  (block
    (thisClient.msgClient (@"(distance-from ({0}) {1})" (str agent)(str dist)) )
    )
 )

;  (on-avatar-pos agent vector) -> "(position (agent) vector)";
(def (on-avatar-pos agent vector)
  (block
    (thisClient.msgClient (@"(position ({0}) '{1}')" (str agent)(str vector)) )
    )
 )

;  (on-avatar-posture agent sitstand) -> "(posture (agent) sitstand)";
(def (on-avatar-posture agent sitstand)
  (block
    (thisClient.msgClient (@"(posture ({0}) '{1}')" (str agent)(str sitstand)) )
    )
 )

;---------------------------------
; prim descriptions
;---------------------------------
;  (on-prim-description obj primID description) -> "(prim-description (obj) 'description' )";
(def (on-prim-description  obj primID description)
  (block
    (thisClient.msgClient (@"(prim-description ({0}) ({1}) ({2}))" (str obj)(str primID)(str description)) )
    )
 )

;  (on-prim-dist prim-name primID dist) -> "(distance-from-prim (prim-name) (primID) distance)";
(def (on-prim-dist prim-name primID dist)
  (block
    (thisClient.msgClient (@"(distance-from-prim ({0})({1}) {2})" (str prim-name)(str primID)(str dist)) )
    )
 )

;  (on-prim-pos prim-name primID vector) -> "(prim-position (prim-name) (primID) vector)";
(def (on-prim-pos prim-name primID vector)
  (block
    (thisClient.msgClient (@"(prim-position ({0})({1}) '{2}')" (str prim-name)(str primID)(str vector)) )
    )
 )

