
(set unless when-not)

; Naming the main thread allows easier debugging when working with threads.
(unless Thread:CurrentThread.Name
 (Thread:CurrentThread.Name "LispMainThread")
)

; The Xml assembly is already loaded -- inform DotLisp.
; Let's not: it defines 3 types named "Type"! So further references to Type
; need to be fully qualified.
;(load-assembly "System.Xml")

; lists as sets:

;NB member defined in boot.lisp

(def (intersect s1 s2)
 (let (r nil rl nil)
  (dolist e s1
   (when (member e s2)
    (when-not (member e r)
     (if (nil? rl)
      (set rl (list e)
           r  rl)
      (set (rest rl) (list e)
           rl        (rest rl)
  )))))
  r
))

(def (intersection &rest sets)
 (reduce intersect sets)
)

; Also a uniq for elements that are not IComparable
(def (union &rest sets)
 (let (r nil rl nil)
  (dolist e (reduce append sets)
   (when-not (member e r :test eqv?)
    (if (nil? rl)
     (set rl (list e)
          r  rl)
     (set (rest rl) (list e)
          rl        (rest rl)
  ))))
  r
))

; should probably be append, but I want subtract, so add should be the same...
(def-binop (add (s1 Cons.) (s2 Cons.)) (union s1 s2))

; nearly identical to intersect
(def-binop (subtract (s1 Cons.) (s2 Cons.)) 
 (let (r nil rl nil)
  (dolist e s1
   (when-not (member e s2 :test eqv?)
    (when-not (member e r :test eqv?)
     (if (nil? rl)
      (set rl (list e)
           r  rl)
      (set (rest rl) (list e)
           rl        (rest rl)
  )))))
  r
))

(def (subset? s1 s2)
 (cond
  (nil? s1) true
  (nil? s2) false
  :else     (nil? (subtract s1 s2))
))

(def (superset? s1 s2)
 (subset? s2 s1)
)

(def (congruent? s1 s2)
 (when (==(len s1)(len s2))
  (and 
   (subset? s1 s2)
   (subset? s2 s1)
)))

; While defining add methods:
(def-binop (add (e Enum.) (f Enum.))
 (lets
  (t (type-of e)
   u (Enum:GetUnderlyingType t)
  )
  (Enum:ToObject t (+ (u e) (u f)))
))

(def-binop (add (ptr IntPtr.) (i Int32.))
 (IntPtr:op_Explicit (+ ptr.ToInt32 i)))

(def-binop (add (ptr IntPtr.) (i Int64.))
 (IntPtr:op_Explicit (+ ptr.ToInt64 i)))

; lst should contain IComparable elements
(def (uniq lst)
 (let (b (into (ArrayList.) lst))
  (when (positive? b.Count)
   (b.Sort)
   (reduce
    (fn (l n)
     (cond
     (nil? l)
      (list n)
     (eqv? (first (last l)) n)
      l
     :else
      (append l (list n))
    ))
    b
    :init nil
))))


; More maths

; Math:DivRem not available to DotLisp v0.6
(def (mod a b)
 (- a (* (/ a b) b)))

(def (gcf a b)
 (cond
  (== a b) a
  (== b 1) 1
  (== a 1) 1
  (== b 0) a
  (== a 0) b
  (< b a)  (gcf b (mod a b))
  :else    (gcf (mod b a) a)))

(def (lcm a b)
 (/ (Math:BigMul a b)
  (gcf a b)))

; More setters

(def (set-second lst v) (set (first (rest       lst)) v))

(def-setter 'second 'set-second)

(def (set-third  lst v) (set (first (nth-rest 2 lst)) v))

(def-setter 'third 'set-third)

(def (set-fourth lst v) (set (first (nth-rest 3 lst)) v))

(def-setter 'fourth 'set-fourth)

(def (set-nth n lst v)
 (if (< n 0)
  (nth n lst)
  (let (r (nth-rest n lst))
   (set (first r) v)
)))

(def-setter 'nth 'set-nth)

(def (set-nth-rest n lst r)
 (if (< n 1)
  (nth-rest n lst)
  (let (l (nth-rest (- n 1) lst))
   (set (rest l) r)
)))

(def-setter 'nth-rest 'set-nth-rest)

(def-macro (set-last lst n &opt v)
 (if (missing? v)
 `(set (last ~lst 1) ~n)
  (let (lstv (gensym) lastv (gensym))
  `(lets (~lstv ~lst
          ~lastv (last ~lstv ~n))
    (if (and ~lstv ~lastv (not(eql? ~lstv ~lastv)))
     (set (rest (last ~lstv (+ ~n 1))) ~v)
     (last ~lstv ~n))))))

(def-setter 'last 'set-last)

; String$ from VB...
(def (strdup c n)
 (String. (0 c) n)
)

; Override (str String.) as defined in boot.lisp:
(def-method (str (obj String.)) 
 (let 
  (controlchars "\\\n\r\t\""
   escapeletters "\\nrt\"" 
   t  obj)
  (for (i 0) (< i controlchars.Length) (++ i)
   (set t (t.Replace
           (.ToString (i controlchars))
           (+ "\\" (i escapeletters))
          )
  ))
  (+ "\"" t "\"" )
))

; Writing strings, not values

(def (w &rest r)
 (for-each s r
  (if (nil? s)
   (prs "nil")
   (prs s)
)))

; Reflection

(def (attributes-of obj)
 (for-each a (.GetCustomAttributes (type-of obj) false)
  (_wa (type-of a) a)
))

(def (attributes obj)
 (for-each a (obj.GetCustomAttributes false)
  (_wa (type-of a) a)
))

(def (_wa t a)
 (w " <"
  (lets
   (n t.Name
    l n.Length
    A "Attribute"
    L A.Length
    B (- l L)
   )
   (if (and (positive? B) (eqv? A (n.Substring B L)))
    (n.Substring 0 B)
    n
 )))
 (for-each p t.GetProperties
  (when-not (== p.Name "TypeId")
   (w " " p.Name "=")
   (_wv p a) ; System.Data has PropertyAttributes too!
   (when-not (== p.Attributes System.Reflection.PropertyAttributes:None)
    (w "[" p.Attributes "]")
 )))
 (w ">\n")
)

(def (_wv p a)
 (pr (p.GetValue a nil))
)

(def (_wf p a)
 (pr (p.GetValue a))
)

; Doesn't include inherited members that have not been overridden -- need to specify BindingFlags:FlatternHierarchy

(def (properties type &key (show-attributes false) (show-private false))
 (w type "\n")
 (for-each p (type.GetProperties (bit-or (bit-or BindingFlags:Static BindingFlags:Instance)(if show-private BindingFlags:NonPublic BindingFlags:Public)))
  (when show-attributes (attributes p))
  (w p " ")
  (let (c p.GetIndexParameters.Length)
   (if (positive? c)
    (w "(" (strdup "," (- c 1)) ")")
  ))
  (w "\n")
))

(def (fields type &key (show-attributes false) (show-private false))
 (w type "\n")
 (for-each p (type.GetFields (bit-or (bit-or BindingFlags:Static BindingFlags:Instance)(if show-private BindingFlags:NonPublic BindingFlags:Public)))
  (when show-attributes (attributes p))
  (w p "\n")
))

(def (constructors type &key (show-attributes false) (show-private false))
 (w type "\n")
 (for-each p (type.GetConstructors (bit-or (bit-or BindingFlags:Static BindingFlags:Instance)(if show-private BindingFlags:NonPublic BindingFlags:Public)))
  (when show-attributes (attributes p))
  (w p "\n")
))

(def (events type &key (show-attributes false) (show-private false))
 (w type "\n")
 (for-each p (type.GetEvents (bit-or (bit-or BindingFlags:Static BindingFlags:Instance)(if show-private BindingFlags:NonPublic BindingFlags:Public)))
  (when show-attributes (attributes p))
  (w p
   (let
    (i  (.ToString (p.EventHandlerType.GetMethod "Invoke"))
     vi "Void Invoke"
     ln 11 ; vi.Length
    )
    (if (eqv? (i.Substring 0 ln) vi) 
     (i.Substring ln)
     (+ " " i)
   ))
   "\n"
)))

(def (event-info type name &key (show-attributes false))
 (lets
  (e (type.GetEvent name)
   t e.EventHandlerType
  )     
  (when show-attributes (attributes e))
  (prnf "{0} {{{1}}}"
   (.Replace (.ToString (t.GetMethod "Invoke")) "Invoke(" (+ name "("))
   t.FullName
)))

(def (methods type &opt (brief false) &key (show-attributes false) (show-private false))
 (w type "\n")
 (let (bf (bit-or (bit-or BindingFlags:Static BindingFlags:Instance)(if show-private BindingFlags:NonPublic BindingFlags:Public)))
  (if (not brief)
   (for-each m (type.GetMethods bf)
    (when show-attributes (attributes m))
    (w m "\n")
   )
   (for-each m (uniq (map1 .Name (type.GetMethods bf)))
    (w m "\n")
   )
)))

(def (methods-of obj &opt (brief false) &key (show-attributes false) (show-private false))
 (methods (type-of obj) brief :show-attributes show-attributes :show-private show-private)
)

(def (constructors-of obj &key (show-attributes false) (show-private false))
 (constructors (type-of obj) :show-attributes show-attributes :show-private show-private)
)

(def (events-of obj &key (show-attributes false) (show-private false))
 (events (type-of obj) :show-attributes show-attributes :show-private show-private)
)

(def (fields-of obj &key (show-attributes false) (show-private false) (static false))
 (let (type (if static obj (type-of obj)))
  (w type.FullName "\n")
  (for-each p (type.GetFields (bit-or (if static BindingFlags:Static BindingFlags:Instance)(if show-private BindingFlags:NonPublic BindingFlags:Public)))
   (when show-attributes (attributes p))
   (when p.IsInitOnly (w "ReadOnly "))
   (w p " = ")
   (_wf p (when-not static obj))
   (w "\n")
)))

(def (properties-of obj &key (show-attributes true) (show-private false) (static false))
 (let (type (if static obj (type-of obj)))
  (w type.FullName "\n")
  (for-each p (type.GetProperties (bit-or (if static BindingFlags:Static BindingFlags:Instance)(if show-private BindingFlags:NonPublic BindingFlags:Public)))
   (if show-attributes (attributes p))
   (w p " ")
   (let (c p.GetIndexParameters.Length)
    (if (positive? c)
     (w "(" (strdup "," (- c 1)) ")")    
     (try
      (block
       (w "= ")
       (_wv p (when-not static obj))
      )
     :catch
      (w "{" (sex ex) "}")
    ))
    (w "\n")
))))

(def-macro (private-property prop obj &key (static false))
 (let (pname (if (is? prop CLSInstanceSymbol.)
    (prop.ToString.Substring 1)
    prop)
   ov (gensym))
  `(let (~ov ~obj)
   (.GetValue (.GetField (type-of ~ov)
     ~pname
     ~(bit-or BindingFlags:NonPublic (if static BindingFlags:Static BindingFlags:Instance)))
    ~ov))))

(def-macro (private-field fld obj &key (static false))
 (let (fname (if (is? fld CLSInstanceSymbol.)
    (fld.ToString.Substring 1)
    fld)
   ov (gensym))
  `(let (~ov
    ~obj)
   (.GetValue (.GetField (type-of ~ov)
     ~fname
     ~(bit-or BindingFlags:NonPublic (if static BindingFlags:Static BindingFlags:Instance)))
    ~ov))))

(def (type-info-of obj &key (show-attributes false)); (show-private))
 (type-info (type-of obj) :show-attributes show-attributes); :show-private show-private)
)

(def (is-delegate t)
 (and (t.IsSubclassOf Delegate.)
      (not (eql? t MulticastDelegate.))
))

(def (type-info t &key (show-attributes false)); (show-private false))
 (let
  (base-type t.BaseType
   delegate (is-delegate t)
  )
  (when show-attributes (attributes t))
  (cond
   t.IsSealed            (w "NotInheritable ")
   t.IsInterface         (#| Do Nothing |#)
   t.IsAbstract          (w "MustInherit ")
  )
  (cond
   t.IsPublic            (w "Global ")
   t.IsNestedPublic      (w "Public ")
   t.IsNestedPrivate     (w "Private ")
   t.IsNestedFamily      (w "Protected ")
   t.IsNestedAssembly    (w "Friend ")
   t.IsNestedFamANDAssem (w "Assembly Protected ")
   t.IsNestedFamORAssem  (w "Protected Friend ")
   :else                 (w "Local ")
  )
  (cond
   delegate              (w "Delegate ")
   t.IsEnum              (w "Enum ")
   t.IsValueType         (w "Structure ")
   t.IsInterface         (w "Interface ")
   :else                 (w "Class ")
  )
  (w t.FullName)
  (when-not (or (nil? base-type) (eql? base-type Object.))
   (w ": Inherits " base-type.Name)
  )
  (cond
  t.IsEnum
   (for-each e (Enum:GetNames t)
    (w "\n " e " = " (str (.value__ (Enum:Parse t e))))
   )
  delegate
   (block
    (w "\n")
    (method-info t "Invoke" :show-attributes show-attributes)
   )
  :else
   (block
    (w "\n" (str t.GetInterfaces))
    (w "\nproperties of ")
    (properties t :show-attributes show-attributes)
    (when-not t.IsInterface
     (w "\nfields of ")
     (fields t :show-attributes show-attributes)
     (w "\nconstructors of ")
     (constructors t :show-attributes show-attributes)
    )
    (w "\nmethods of ")
    (methods t :show-attributes show-attributes)
    (w "\nevents of ")
    (events t :show-attributes show-attributes)
    (let (n t.GetNestedTypes)
     (when (and n (positive? n.Length))
      (w "\nnested types:")
      (for-each nt n
       (w "\n" nt)
  )))))
  (w "\n")
))

(def (method-info t name &key (show-attributes false))
 (for-each m t.GetMethods
  (when (eqv? m.Name name)
   (when show-attributes (attributes m))
   (w m "\n"))))

(def (property-info t name &key (show-attributes false))
 (for-each m t.GetProperties
  (when (eqv? m.Name name)
   (when show-attributes (attributes m))
   (w m "\n"))))

(def (field-info t name &key (show-attributes false))
 (for-each m t.GetFields
  (when (eqv? m.Name name)
   (when show-attributes (attributes m))
   (when m.IsInitOnly (w "ReadOnly "))
   (w m "\n"))))

; Without this, (str <some assembly>.Evidence) returns 2MB(!) of base64 policy data wrapped in XML.
(def-method (str (e Evidence.))
 (@ "{{{0}: {1}}}"
  (.Name (type-of e))
  (str (map->list type-of e))
))

(def (property-of obj name &key (show-attributes false))
 (for-each p (.GetProperties (type-of obj))
  (when (eqv? p.Name name)
   (when show-attributes (attributes p))
   (w p " ")
   (let (c p.GetIndexParameters.Length)
    (if (positive? c)
     (w "(" (strdup "," (- c 1)) ")")    
     (try
      (block
       (w "= ")
       (wv p obj)
      ) :catch
      (w "{" (sex ex) "}")
   )))   
   (w "\n")
)))

; COM Late-binding

(def-macro (create-object Prog-Id) `((Type:GetTypeFromProgID ~Prog-Id)))

(def-macro (late method object &rest arguments)
 (lets
  (obj (gensym)
   ms  (if (cons? method)
        method
        (let (m (method.ToString))
         (if (eqv? (0 m) (0 "."))
          (m.Substring 1)
          m)))
  )
 `(let (~obj ~object)
   (.InvokeMember (type-of ~obj)
    ~ms
    BindingFlags:InvokeMethod
    Type:DefaultBinder
    ~obj
    (vector-of Object. ~@arguments)
))))

(def-macro (late-get method object &rest arguments)
 (lets
  (obj (gensym)
   ms  (if (cons? method)
        method
        (let (m (method.ToString))
         (if (eqv? (0 m) (0 "."))
          (m.Substring 1)
          m)))
  )
 `(let (~obj ~object)
   (.InvokeMember (type-of ~obj)
    ~ms
    BindingFlags:GetProperty
    Type:DefaultBinder
    ~obj
    (vector-of Object. ~@arguments)
))))

(def-macro (late-set method object &rest arguments)
 (lets
  (obj (gensym)
   ms  (if (cons? method)
        method
        (let (m (method.ToString))
         (if (eqv? (0 m) (0 "."))
          (m.Substring 1)
          m)))
  )
 `(let (~obj ~object)
   (.InvokeMember (type-of ~obj)
    ~ms
    BindingFlags:SetProperty
    Type:DefaultBinder
    ~obj
    (vector-of Object. ~@arguments)
))))

; IJW??? See get-hidden-interfaces for proof and other examples...
(def-method (get-enum (obj __ComObject.))
 (cond (is? obj IEnumerator.) obj
 (is? obj IEnumerable.)
  (.IEnumerable:GetEnumerator obj)
 :else
  (error (@"Error - no method found matching argument: {0}"(str obj)))))

; NB this works (when it should) but does NOT return the new value like set should.
(def-setter 'late-get 'late-set)

; GetObject is supplied by the VB runtime...
(def (get-object Prog-Id &opt (document "")) (Interaction:GetObject document Prog-Id))

; Sample:
;(set FSO (create-object "Scripting.FileSystemObject"))
;(set textStream (late OpenTextFile FSO "queens.lisp"))
;(until (late-get AtEndOfStream textStream) (prns (late ReadLine textStream)))
;(late Close textStream)
;(set textStream nil)
;(set FSO nil)
; NB Marshal:ReleaseComObject can accellerate the releasing of the COM object.

(def (__member? sym)
 (and (is? sym DotLisp.CLSInstanceSymbol.)
  (is? (eval sym) DotLisp.CLSLateBoundMember.)))

; Simple pretty printer
; Due to an 'invalid' algorithm, this no longer puts right brackets on a new line...
(def (pp l &opt (indent 0) (is-first false))
 (let (rb nil) ; nil means "caller" do nothing; 0 -> " "; n -> n ")"s
  (cond
  (or (missing? l) (nil? l) (atom? l))
   (block
    (w (str l))
    (when (positive? indent)
     (set rb 0)
   ))

  (cons? l)
   (when (case (first l)

    (quote backquote unquote unquote-splicing)
    ; without knowing the context (quote x) -> 'x, but it doesn't matter:
    ; (case 'quote 'x 'match :else 'nomatch) --> match
    ; (case 'x 'x 'match :else 'nomatch) --> match
    ; (case 'x `x 'match :else 'nomatch) --> match
    ; (case 'x ~x 'match :else 'nomatch) --> match
    ; (case 'x ~@x 'match :else 'nomatch) --> match
    ; (case 'unquote ~x 'match :else 'nomatch) --> match
    ; (case 'backquote `x 'match :else 'nomatch) --> match
    ; (case 'unquote-splicing ~@x 'match :else 'nomatch) --> match

     (if (and (rest l) (not (rest (rest l))))
      (block
       (w (case (first l)
         quote     "'"
         backquote "`"
         unquote   "~"
         unquote-splicing "~@"
       ))
       (set rb (pp (second l) indent true))
       (when-not (or rb (zero? indent))
        (set rb 0)
       )
       false
      )
      true
     )

    vector
    ; Cannot differentiate between [] and (vector), but it doesn't matter:
    ; (case 'vector [] 'match :else 'nomatch) -> match

     (let (i (+ indent 1))
      (w "[")
      (dolist m (rest l)
       (set rb (pp m i))
       (if (and rb (positive? rb))
        (w (strdup ")" rb))
        (w " ")
      ))
      (w "]")
      (when (positive? indent)
       (set rb 0)
      )
      false
     )

    :else
      true
    )
    (let
     (cnt (len l)
      i   (+ indent 1)
      blk (and (rest l) (eqv? (first l) 'block))
      ; check for .Method symbols followed by an atom (.x y z...) -> (y.x z...) & (.x y) -> y.x
      ; nb (.x (.y (.z w))) -> (w.z.y.x) and (.x (.y (.z w)) v) -> (w.z.y.x v)
      ; but (.x (.y (.z w) v)) -> (.x (w.z.y v))
      ; So dot processing if second is atom or a 2 item list starting with a .Method and the second applies recursively.
      ; So every (or (not(cons? s))(and(not(nil?(rest s)))(nil?(rest(rest s)))(is? (first s) DotLisp.CLSInstanceSymbol.))
      dot (and (rest l) (__member? (first l)) ; this level doesn't need len=2
           (every (fn (s)
             (or (not (cons? s))
              (and (not (nil? (rest s)))
                   (nil? (rest(rest s)))
                   (__member? (first s)))))
            (make-enum
             (s l)
             s
             (if (and (cons? s)(rest s))
              (block
               (set s (second s))
              true)
              false))))
      ;dot (and (rest l) (__member? (first l)) (not (cons? (second l))))
      r2l (rest(rest l))
      nsp true
     )
     (or (and dot (nil? r2l) (not is-first))
      (w "("))
     (dolist m (if dot (cons(second l)(cons(first l)r2l))l)
      (set rb (pp m i (and nsp (not dot))))
      (-- cnt)
      (when (and(positive? cnt) (not (and dot nsp)))
       (if (or blk (cons? m) (and rb (positive? rb)))
        (block
         (w "\n" (strdup " " i))
         (when (and rb (positive? rb))
          (w (strdup ")" rb) "\n" (strdup " " i))
        ))
        (when rb
         ;(assert (zero? rb))
         (w " ")
      )))
      (when nsp(set nsp nil))
     )
     (or (and dot (nil? r2l) (not is-first))
      (if (or (nil? rb) (zero? rb))
       (block
        (w ")")
        ; Ensure a space (or more right brackets)
        (set rb 0)
       )
       ; else (positive? rb)
       (++ rb)
   ))))
  :else
   (block
    (beep)
    (w "\npp?:")
    (prn l)
  ))
  (when (and rb (zero? indent))
   (w "\n" (strdup ")" rb))
   (set rb nil)
  )
  rb
))

; simple exception string
; (def-method str (ex Exception.) ... don't want to lose the built-in one.
(def (sex ex &opt (base true))
 (if (nil? ex)
  "nil"
  (if base
   (sex ex.GetBaseException false)
   (+ (.Name (type-of ex)) ": " ex.Message)
)))

; print detailed exception
(def (dex ex &opt (base true))
 (if base
  (dex ex.GetBaseException false)
  (w (.FullName (type-of ex))
   "\nMessage: " ex.Message
   "\nTarget: " ex.TargetSite
   "\nSource: " ex.Source
   "\n"
)))

; returns nil unless for some n & m (eql? (nth-rest n lst) (nth-rest m lst))
(def (circular? lst)
 (let (c 0)
  (for (p (rest lst) q (rest p))
   (not (or (when (nil? q  ) (set c nil) true)
            (when (eql? p q) (++ c)      true)
   )    )
   (set p (rest p)   q (rest (rest q)))
   (++ c)
  )
  c
))

(def (between x lower upper)
 (to-bool
  (and
   (or (missing? lower) (<= lower x))
   (or (missing? upper) (<= x upper))
)))

; Deliberately *not* using *pr-writer...
(def (beep) (Console:Out.Write (Char.  7)))
;(def (cls) (Console:Out.Write (Char. 12)))

; Better than boot.lisp using...

(def-macro (__using var val &rest body)
 (let (s (gensym))
 `(lets
   (~s ~val
    ~var ~s
   )
   (try
    (block ~@body)
   :finally
    (when (and ~s (is? ~s IDisposable.))
     (.IDisposable:Dispose ~s)
)))))

(def-macro (with-dispose inits &rest body)
 (cond
 (nil? inits)
 `(block ~@body)
 (nil? (rest inits))
  (error "with-dispose: odd args")
 :else
 `(__using ~(first inits) ~(second inits)
   (with-dispose ~(nth-rest 2 inits) ~@body)
  )
))

(def-macro (pipe-string &rest body)
 (let (sw (gensym))
 `(with-dispose (~sw (StringWriter.))
   (dynamic-let (*pr-writer ~sw)
    ~@body
    (.ToString ~sw)
))))

(def (enum-stream stream)
 (make-enum
  (c nil)
  c
  (set c (stream.ReadLine))
  (not (nil? c))
))

(def-macro (enum-string string)
`(enum-stream (StringReader. ~string))
)

(def-macro (pipe &rest body)
`(enum-string (pipe-string ~@body))
)

; needs a better name for first recall
(def (enum-pos string target &opt (start 0))
 (unless (and target target.Length) (error "enum-pos: target must not be empty"))
 (make-enum (l target.Length pos (- start l)) pos
  (set pos (.IndexOf string target (+ pos l)))
  (not (negative? pos))))

(def-macro (for-each-index-of pos string target &rest body)
`(for-each ~pos (enum-pos ~string ~target)
 ~@body))

; Same as len, but works for sequences (enums).
; Of course, it does "eat" the sequence.
(def (count seq)
 (let
  (seq (get-enum seq)
   c 0)
  (while (.IEnumerator:MoveNext seq)
   (++ c))
  c))

;typing saver: you've tried .a.b.c and found you need (.c .a.b) or some such
; now you can type (, .a.b .c)
(def-macro (, a b &rest r) `(~b ~a ~@r))

; Exception -> String
(def-macro (E &rest body)
 (let (r (gensym))
 `(let (~r nil)
   (try
    (set ~r (block ~@body))
   :catch
    (set ~r (sex ex))
   )
  ~r
)))

; Exception -> nil
(def-macro (P &rest body)
 (let (r (gensym))
 `(let (~r nil)
   (try
    (set ~r (block ~@body))
   :catch
    (set ~r nil)
   )
  ~r
)))

; extend into to work with types that are of fixed size
(def-method (into (type System.Type.) seq)
 (cond
 (eql? type Array.)
  (.ToArray (into (ArrayList.) seq))
 (.IsArray type)
  (lets
   (lst (into (ArrayList.) seq)
    arr (Array:CreateInstance type.GetElementType lst.Count))
   (lst.CopyTo arr)
   arr
  )
 :else
  (into (type) seq)
))

; Type searches

(def-macro (for-each-type cur-type &rest body)
 (let (cur-assembly (gensym))
 `(for-each ~cur-assembly AppDomain:CurrentDomain.GetAssemblies
   (for-each ~cur-type (P(.GetTypes ~cur-assembly))
   ~@body
))))

(def (enum-all-types)
 (make-enum
  (assemblies (get-enum AppDomain:CurrentDomain.GetAssemblies)
   cur-type nil)
  cur-type.Current
  (while (and assemblies (not (and cur-type cur-type.MoveNext)))
   (if assemblies.MoveNext 
    (set cur-type (P (get-enum (P assemblies.Current.GetTypes))))
    (set assemblies nil)
  ))
  (to-bool assemblies)
))

(def (search-types s &opt (brief true) &key (show-attributes true))
 (for-each-type t
  (when-not (negative? (t.Name.IndexOf s))
   (if (or (missing? brief) (not brief))
    (type-info t :show-attributes show-attributes)
    (w t "\n")
))))

(def (search-types-for m &opt (brief true) &key (show-attributes false) (show-private false))
 (for-each-type t
  (let (q nil)
   (when
    (any (fn (p)
      (if (negative? (p.Name.IndexOf m))
       false
       (block (set q p) true)
     ))
     (reduce (fn (x y) (x.AddRange y) x)
      (let (bf (bit-or (bit-or BindingFlags:Static BindingFlags:Instance)(if show-private BindingFlags:NonPublic BindingFlags:Public)))
       (list (t.GetProperties bf) (t.GetFields bf) (t.GetMethods bf))
      ) :init (ArrayList.))
    )
    (if (not brief)
     (type-info t)
     (w t "." q.Name "\n")    
)))))

(def (list-namespace s)
 (for-each-type t
  (when (eqv? t.Namespace s)
   (w t "\n")    
)))

(def (get-hidden-interfaces obj)
 (let
  (type (type-of obj)
   rslt (ArrayList.))
  (for-each-type t
   (when (and t.IsInterface (is? obj t))
    (rslt.Add t)))
  (- (or (into () rslt) (list 'none))
     (or (into () type.GetInterfaces) '(none)))))

; Current directory -- (set (curdir) "..") works too using the default macro setter processing
(def-macro (curdir &opt new-dir)
 (if (missing? new-dir)
 `Environment:CurrentDirectory
 `(set Environment:CurrentDirectory ~new-dir)
))

(set *dir-list nil)

(def (push-dir new-curdir)
 (let (cur-dir (curdir))
  (if (and new-curdir (positive? new-curdir.Length))
   (block
    (set (curdir) new-curdir)
    (push! cur-dir *dir-list)
    (curdir)
   )
   (error "No path given.")
)))

(def (pop-dir)
 (let (new-dir (first *dir-list))
  (if new-dir (block
    (set (curdir) new-dir)
    (pop! *dir-list)
    (curdir)
   )
   (error "Empty directory stack.")
)))

; Put text file into a string -- analogous to get-web-page
(def (get-file file-name)
 (lets
  (fi (FileInfo. file-name)
   l  (Int32. fi.Length))
  (with-dispose
   (f (.OpenText fi))
   (lets
    (b (Array:CreateInstance Char. l)
     m (f.Read b 0 l)
     s (String. b 0 m))
    f.Close
    s
))))


; Temporary file:
(def-macro (with-temp-file file-name &rest body)
 (let (filename (gensym) fi (gensym))
 `(lets (~filename (Path:GetTempFileName) ~fi (FileInfo. ~filename))
   (try
    (let (~file-name ~filename)
    ~@body)
   :finally
    (block
     (.Refresh ~fi)
     (when (.Exists ~fi)
      (.Delete ~fi)
))))))

; Enumerating all files
; Need to wrap in try :catch

(def (for-all-drives f &opt (skip-missing true))
 (for-each d (map1 DirectoryInfo. (Directory:GetLogicalDrives))
  (when (or (not skip-missing) d.Exists)
   (f d)
)))

(def-macro (for-each-drive f &rest body)
`(for-all-drives
  (fn (~f)
   ~@body
)))

(def-macro (for-each-folder d name-list &rest body)
`(for-each ~d (map1 DirectoryInfo. ~name-list)
  ~@body
))

(def-macro (for-each-file f name-list &rest body)
`(for-each ~f (map1 FileInfo. ~name-list)
  ~@body
))

(def-macro (for-every-file f dir &key (on-error :continue-with-msg) &rest body)
 (let
  (rsym (gensym)
   dsym (gensym)
   ssym (gensym)
   df   (fn (d)
    (if (is? d DirectoryInfo.)
     d
     (DirectoryInfo. (d.ToString))
  )))
 `(letfn
   ((~rsym ~dsym)
    (block
     (try
      (for-each ~f (.GetFiles ~dsym)
       ~@body
      )
     :catch
      (~@(case on-error
       (:continue)          `nil
       (:continue-with-msg) `(prns (sex ex))
       (:fail)              `(throw ex)
       :else                on-error
     )))
     (try
      (for-each ~ssym (.GetDirectories ~dsym)
       (~rsym ~ssym)
      )
     :catch
      (~@(case on-error
       (:continue)          `nil
       (:continue-with-msg) `(prns (sex ex))
       (:fail)              `(throw ex)
       :else                on-error
   )))))
   (~rsym (~df ~dir))
)))

(def-macro (for-all-local-files f &rest body)
 (let (dsym (gensym))
 `(for-each-drive ~dsym
   (for-every-file ~f ~dsym ~@body)
)))

(def (look-in dir file-name)
 (look-for (DirectoryInfo. dir) file-name)
)

(def (look-for dir file-name)
 (try
  (for-each f dir.GetFiles
   (when-not (negative? (f.Name.ToLower.IndexOf file-name))
    (w f.FullName "\n")
  ))
 :catch
  (w (sex ex) "\n")
 )
 (try
  (for-each d dir.GetDirectories
   (look-for d file-name)
  )
 :catch
  (w (sex ex) "\n")
))

(def (search-in dir ftype pred)
 (search-for (DirectoryInfo. dir) ftype pred)
)

(def (search-for dir ftype pred)
 (try
  (for-each f (dir.GetFiles ftype)
   (when (P (pred f))
    (w (E f.FullName) "\n")
  ))
 :catch
  (w (E (sex ex)) "\n")
 )
 (try
  (for-each d dir.GetDirectories
   (search-for d ftype pred)
  )
 :catch
  (w (E (sex ex)) "\n")
))

; Sample use:
;(search-in "f:\\dotnet\\cstools3" "*.cs"
; (fn (f)
;  (let (r false)
;   (with-dispose (s f.OpenText)
;    (set r (negative? (s.ReadToEnd.IndexOf "cs0")))
;    s.Close
;   )
;   (not r)
;)))

; Format expressions

(def (@ expr &rest args)
 (String:Format expr (apply vector-of Object. args))
)

(def (prnf expr &rest args)
 (prns (apply @ expr args))
)

(def (prf expr &rest args)
 (prs (apply @ expr args))
)

; Histograms

(def (histo lst &key (sorted true))
 (let (h (Hashtable.))
  (for-each e lst
   (e h (+ 1 (or (e h) 0)))
  )
  (if sorted
   (SortedList. h)
   h
)))

(def (print-histo h &key (sort-count false))
 (if sort-count
  (histo-print h)
  (for-each k (.Keys h)
   (prnf "{0,4} {1}" (k h) k)
)))

(def (histo-print h &key (sort-values false))
 (if sort-values
  (print-histo h)
  (let
   (karr (into Array. h.Keys)
    varr (into Array. h.Values))
   (Array:Sort varr karr)
   (for-each k karr
    (prnf "{0,4} {1}" (k h) k)
))))

; Execute command
(def (run cmd &opt (args "") &key (wait true))
 (with-dispose (p (Process.))
  (let (start-info p.StartInfo)
   (start-info.FileName   cmd)
   (start-info.Arguments  args)
   (start-info.UseShellExecute        false)
   (start-info.RedirectStandardOutput true)
   (start-info.RedirectStandardError  true)
   (if (and p.Start wait)
    (list
     p.StandardOutput.ReadToEnd
     p.StandardError.ReadToEnd
     p
    )
    p
))))

; Records
(def-method (str (r DotLisp.Record.))
 (apply +
  (str (type-of r)) "\r\n"
  (mapcat! (fn (k)
    (list k " " (str (k r)) "\r\n"))
   (.Record:Keys r))))

;; System.Data.Common.DbDataRecord
;(def-method (str (r System.Data.Common.DbDataRecord.))
; (apply +
;  (str (type-of r)) "\r\n"
;  (mapcat! (fn (k)
;    (list (.System.Data.Common.DbDataRecord:GetName r k) " " (str (k r)) "\r\n"))
;   (range 0 (.System.Data.Common.DbDataRecord:FieldCount r)))))


(def (help)
 (run "explorer" "F:\\DOTNET\\DotLisp\\DotLisp.htm" :wait false))

(def (edit filename &key (wait false))
 (run "notepad" filename :wait wait))

(def (explore uri)
 (run "explorer" (@ "\"{0}\"" uri) :wait false))

(def (tvguide-url &opt date)
 (@ "http://tvguide.ninemsn.com.au/guide/{0:ddMMyyyy}_81.asp?channel=free&day={0:d/M/yyyy}"
  (if (missing? date)
   (.AddHours (now) -6)
   date
)))

(def (tvguide &opt date)
 (explore (tvguide-url date)))

; Sequences -- ordered sets (of integers)

(def (ranges seq)
 (let
  (firsts (- seq (map->list (fn (x) (+ x 1)) seq))
   lasts  (- seq (map->list (fn (x) (- x 1)) seq))
   result nil
  )
  (for () (not (nil? firsts)) (block (next! firsts) (next! lasts))
   (let (rng (list (list (first firsts) (first lasts))))
    (if (nil? result)
     (set result rng)
     (+=  result rng)
  )))
  result
))

; application of sequences:
(def (diff text1 text2 )
 (let
  (M    (min text1.Length text2.Length)
   L    (max text1.Length text2.Length)
   difs nil
  )
  (for (i 0) (< i L) (++ i)
   (when-not
    (and (< i M )
     (eqv? (i text1) (i text2))
    )
    (if (nil? difs)
     (set difs (list i))
     (+=  difs (list i))
  )))
  difs
))

; Curried functions: ((f a) b) === (f a b)
(def-macro (curry-fn (&rest formals) &rest body)
 (with-gensyms (args-needed self-currying args extra this)
  `(let (~args-needed (len '~formals))
   (letfn(
    (~this ~@formals)
     (block ~@body)
    (~self-currying &rest ~args)
     (let (~extra (- (len ~args) ~args-needed))
      (cond (zero? ~extra)
       (apply ~this ~args)
      ; This will allow for compound currying, but causes
      ; errors when too many arguments are supplied.
      (positive? ~extra)
       (apply (apply ~this (butlast ~args ~extra)) (last ~args ~extra))
      :else ; negative
       (fn (&rest more-args)
        (apply ~self-currying
         (append ~args
          more-args))))))
    ~self-currying))))

(def-macro (def-curry (name &rest formals) &rest body)
`(set ~name (curry-fn ~formals ~@body)))

; Reader
(def (read-from-string s &key (stream "read-from-string"))
 (with-dispose (r (StringReader. s))
  (try
   (interpreter.Read stream r)
  :finally
   r.Close
)))

(def (read-from-string-all s &key (stream "read-from-string-all"))
 (read-from-string (@ "({0}\r\n)" s) :stream stream)
)

(def (read-eval-string-all s &key (stream "read-eval-string-all"))
 (with-dispose (r (StringReader. s))
  (try
   (for (e (interpreter.Read stream r)) (not (interpreter.Eof e)) (set e (interpreter.Read stream r))
    (eval e)
   )
  :finally
   r.Close
)))

(def (read-expand-file file-name &key (pretty false))
 (let (prt (if pretty pp prn))
  (with-dispose
   (f (.OpenText (FileInfo. file-name)))
   (for (x (interpreter.Read file-name f))
        (not (interpreter.Eof x))
        (set x (interpreter.Read file-name f))
    (let
     (prev nil
      sexp x)
     (until (eqv? prev (set sexp (macroexpand-1 sexp)))
      (set prev sexp)
     )
     (prt sexp)
)))))

(def (macroexpand form &key (symplify true))
 (for (f form)
  (and
    (cons? form)
    (not (eqv? form
     (set f (macroexpand-1 form))
  )))
  (set form f)
 )
 (when symplify
  (when (cons? form)
   (case (first form)
   (block)
    (let
     (lst
      (mapcat!
       (fn (f)
        (if (and(cons? f)(eqv? (first f) 'block))
         (rest f)
         (list f)
       ))
       (rest form)
     ))
     (if (rest lst)
      (set form (cons 'block lst))
      (set form (first lst))
    ))
 )))
 form
)

(def (macroexpand-all form)
 (for (f form)
  (and
    (cons? form)
    (not (eqv? form
     (set f (macroexpand form))
  )))
  (set form f)
 )
 (if (cons? form)
  (case (first form)
  (quote)
   form
  (fn)
   (apply list (first form) (second form)
    (map->list macroexpand-all
     (rest (rest form))
   ))
  :else
   (map->list macroexpand-all form)
  )
  form
))

; Get a web page:
(def (get-web-page url)
 (Encoding:ASCII.GetString (with-dispose (web (WebClient.))
   (web.DownloadData url))))

(def-macro (with-web-page-and-headers url headers page &rest body)
 (let (web (gensym))
 `(with-dispose (~web (WebClient.))
   (lets (~page (.DownloadData ~web ~url)
    ~headers (.ResponseHeaders ~web))
   ~@body
))))

; Regular Expressions:
(def-macro (enum-matches re s)
`(make-enum (next-match nil) next-match
  (.Success
   (set next-match
    (if (nil? next-match)
     (.Match ~re ~s)
     next-match.NextMatch)))))

(def-macro (for-each-match m re s &rest body)
`(for-each ~m (enum-matches ~re ~s)
  ~@body
))

; Simple word-wrapping
(def (word-wrap text &key (line-length 79))
 (for-each-match m (Regex. (@ ".{{1,{0}}}(\\s|$)" line-length) RegexOptions:Singleline) text
  (prns m.Value)))

; Cleaning spaces
(def (consolidate-whitespace text)
 (.Replace (Regex. "(\\s|([&]nbsp[;]))+") text " ")
)

; Ignoring <tags>
(def (remove-tags text)
 (.Replace (Regex. "<([^\"'>]|\"[^\"]*\"|'[^']*')*>") text "")
)

; Listing <tags>
(def (enum-tags text)
 (map .Value (enum-matches (Regex. "<([^\"'>]|\"[^\"]*\"|'[^']*')*>") text))
)

; Decoding base64 text
(def (decode-base64 text)
 (Encoding:ASCII.GetString (Convert:FromBase64String (.Replace text "!" "")))
)

; Decoding quoted-printable
(def (decode-qp text)
 (lets
  (qpr
   (- (let (s (ArrayList.))
     (for-each-match m (Regex. "=(.|\r|\n)(.|\r|\n)") text
      (s.Add (String:Intern m.Value))
     )
     (uniq s)
    )
    (list
     (String:Intern "=\r\n")
     (String:Intern "=3D")
   ))
   utxt (StringBuilder. text)
  )
  (for-each r qpr
   (utxt.Replace r (String. (Char. (0 (Hex:DecodeHexString (r.Substring 1)))) 1))
  )
  (utxt.Replace "=\r\n" "" )
  (utxt.Replace "=3D"   "=")
  utxt.ToString
))

; Typename -> type
(def (T type-name) (Type:GetType type-name))
(def-method (ref (type Type.  )) (ref type.FullName))
(def-method (ref (type String.)) (T (+ type "&")))
(def-method (ptr (type Type.  )) (ptr type.FullName))
(def-method (ptr (type String.)) (T (+ type "*")))
(def-method (arr (type Type.  ) &opt (rank 1)) (arr type.FullName rank))
(def-method (arr (type String.) &opt (rank 1)) (T (+ type "[" (strdup "," (- rank 1)) "]")))

; P/Invoke:
(when-not (.isDefined '__PInvoke-assembly)
 (set __PInvoke-assembly 
  (let (assembly-name (AssemblyName.))
   (assembly-name.Name "PInvokeAssembly")
   ; Is there a semantic difference between (Thread:GetDomain) and AppDomain:CurrentDomain? No.
   (AppDomain:CurrentDomain.DefineDynamicAssembly assembly-name AssemblyBuilderAccess:Run)
 ))

 (set __PInvoke-module (__PInvoke-assembly.DefineDynamicModule "PInvokeModule"))
 (set __PInvoke-type   nil)
 (set __PInvoke-typeno 0)
)

; Would like to use the ModuleBuilder's DefinePInvokeMethod to create global methods.
; But global methods are created only when the module is done. So we need
; multiple Modules, or multiple Types in a module.
;
(def-macro
 (PInvoke-declare Name &key Lib Alias
  (Args Type:EmptyTypes)
  (Return-Type System.Void.)
  (Calling-Convention CallingConvention:Winapi)
  (CharSet CharSet:None)
  (Calling-Conventions CallingConventions:Standard)
  (Attributes (+ MethodAttributes:PinvokeImpl MethodAttributes:HideBySig MethodAttributes:Static MethodAttributes:Public))
  (MethodImplAttributes MethodImplAttributes:PreserveSig)
 )
 (when-not (symbol? Name)
  (error "Name must be a Symbol.")
 )
 (when-not (is? Lib String.)
  (if (or (missing? Lib) (nil? Lib))
   (error "Lib must be specified.")
   (error "Lib must be a String.")
 ))
 (if (nil? __PInvoke-type)
  (set __PInvoke-type (__PInvoke-module.DefineType (+ "PInvokeType" (.ToString (++ __PInvoke-typeno)))))
  (__PInvoke-type.DefineDefaultConstructor MethodAttributes:Private)
 )
 (let (mb (gensym) args (gensym) name Name.ToString)
 `(block
   (lets
    (~args ~Args     
     ~mb   (__PInvoke-type.DefinePInvokeMethod ~name ~Lib ~@(when-not (missing? Alias) (list Alias)) ~Attributes ~Calling-Conventions ~Return-Type ~args ~Calling-Convention ~CharSet)
    )
    (.SetImplementationFlags ~mb ~MethodImplAttributes)
   )
   (set ~Name
    (fn (&rest arg-list)
     (when (and __PInvoke-type
           ; NB .GetHashCode does *not* guarentee uniqueness
           (eql? (__PInvoke-type.GetHashCode) ~(.GetHashCode __PInvoke-type))
           (eql? (String:IsInterned __PInvoke-type.FullName) ~(String:Intern __PInvoke-type.FullName))
      )
      (__PInvoke-type.CreateType)
      (set __PInvoke-type nil)
     )
     (.InvokeMember (.GetType __PInvoke-module ~__PInvoke-type.Name)
      ~name
      BindingFlags:InvokeMethod
      Type:DefaultBinder
      nil
      (apply vector-of Object. arg-list)
))))))

; Registry stuff

; Delay getting handles until needed
(set *std-reg-hive-name (ListDictionary.))
("hkcr" *std-reg-hive-name (fn()Registry:ClassesRoot))
("hkcu" *std-reg-hive-name (fn()Registry:CurrentUser))
("hklm" *std-reg-hive-name (fn()Registry:LocalMachine))
("hku"  *std-reg-hive-name (fn()Registry:Users))
("hkpd" *std-reg-hive-name (fn()Registry:PerformanceData))
("hkcc" *std-reg-hive-name (fn()Registry:CurrentConfig))
("hkdd" *std-reg-hive-name (fn()Registry:DynData))

; Can distinguish between a value of "" and not present, but not necessarily for a key's default value.
(def (reg-value key value-name &opt default-value)
 (lets
  (s (let (s (key.IndexOf "\\")) (if (> s 0) s key.Length))
   hive-name (.ToLower (key.Substring 0 s))
   subkey (key.Substring (+ s 1)))
  (with-dispose
   (hive (or ((.get_Item *std-reg-hive-name hive-name)) (error (@"Unknown registry hive: {0}" (key.Substring 0 s))))
    hkey (hive.OpenSubKey subkey false))
   (if hkey (or (hkey.GetValue value-name) default-value) default-value)
)))

(def (set-reg-value key value-name new-value)
 (lets
  (s (let (s (key.IndexOf "\\")) (if (> s 0) s key.Length))
   hive-name (.ToLower (key.Substring 0 s))
   subkey (key.Substring (+ s 1)))
  (with-dispose
   (hive (or ((.get_Item *std-reg-hive-name hive-name)) (error (@"Unknown registry hive: {0}"(key.Substring 0 s))))
    hkey (hive.CreateSubKey subkey))
   (if hkey (hkey.SetValue value-name new-value) (error (@"Registry key not created: {0}" key)))
)))

;;; This could be the start of a new file here.

; Event log
(def-macro (with-eventlog e
  &key (log "application") (machine ".") (max-count 10)
  &rest body)
 (with-gensyms (ev l i c)
 `(with-dispose (~ev
    (EventLog. ~log ~machine))
   (lets
    (~l (.Entries ~ev)
     ~c (.Count ~l))
    (dotimes ~i (min ~max-count ~c)
     (with-dispose (~e
       ((- ~c ~i 1) ~l))
      ~@body))))))

; Forms stuff

;NB System.Drawing and Accessibility are loaded by loading
; System.Windows.Forms, so informing DotLisp makes sense.
(load-assembly "System.Windows.Forms")
(load-assembly "System.Drawing")
(load-assembly "Accessibility")

; Window clipboard:
(def (clip-text)
 (let (d (Clipboard:GetDataObject))
  (when (and
    (not (nil? d))
    (.IDataObject:GetDataPresent d DataFormats:Text)
   )
   (.IDataObject:GetData d DataFormats:Text)
)))

; clip-copy "data" puts "data" on the clipboard
; clip-copy "data" true ensures the data is available after dotlisp quits.
; ... Looks like .NET clipboard needs a message pump to work right...
(def (clip-copy data &opt (keep true))
 ; Clear clipboard first
 (Clipboard:SetDataObject (DataObject.))
 (Clipboard:SetDataObject data keep))

; NB this works but does NOT return the new value like set should.
(def-setter 'clip-text 'clip-copy)

(def (read-from-clip)     (read-from-string     (clip-text) :stream "clipboard"))
(def (read-from-clip-all) (read-from-string-all (clip-text) :stream "clipboard"))
(def (read-eval-clip-all) (read-eval-string-all (clip-text) :stream "clipboard"))

(set *clip-list nil)

(def (push-clip)
 (let (clip (clip-text))
  (if (and clip (positive? clip.Length))
   (prn (count (push! clip *clip-list)))
   (error "No text on clipboard."))))

(def (pop-clip)
 (let (clip (first *clip-list))
  (if clip (block
    (clip-copy clip)
    (pop! *clip-list)
    (prn (count *clip-list)))
   (error "Empty clipboard stack."))))
