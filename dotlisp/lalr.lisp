;;;  lalr.lisp
;;;
;;;  This is an LALR parser generator.
;;;  (c) 1988 Mark Johnson. mj@cs.brown.edu
;;;  This is *not* the property of Xerox Corporation!
;;;
;;;  http://www.cog.brown.edu/~mj/Software.htm

;;;  Modified to cache the first terminals, the epsilon derivations
;;;  the rules that expand a category, and the items that expand
;;;  a category

;;;  There is a sample grammar at the end of this file.
;;;  Use your text-editor to search for "Test grammar" to find it.

;;; CL Pagage stuff

;;; (in-package 'LALR)
;;; (export '(make-parser lalr-parser *lalr-debug* grammar lexforms $ parse))

;;; (shadow '(first rest))
;;; (defmacro first (x) `(car ,x))
;;; (defmacro rest (x) `(cdr ,x))

;;;  The external interface is MAKE-PARSER.  It takes three arguments, a
;;;  CFG grammar, a list of the lexical or terminal categories, and an
;;;  atomic end marker.  It produces a list which is the Lisp code for
;;;  an LALR(1) parser for that grammar.  If that list is compiled, then
;;;  the function LALR-PARSER is defined.  LALR-PARSER is a function with 
;;;  two arguments, NEXT-INPUT and PARSE-ERROR. 
;;;
;;; MEH: You just need to eval the output for DotLisp.
;;;
;;;  The first argument to LALR-PARSER, NEXT-INPUT must be a function with 
;;;  zero arguments; every time NEXT-INPUT is called it should return
;;;  a CONS cell, the CAR of which is the category of the next lexical
;;;  form in the input and the CDR of which is the value of that form.
;;;  Each call to NEXT-INPUT should advance one lexical item in the
;;;  input.  When the input is consumed, NEXT-INPUT should return a
;;;  CONS whose CAR is the atomic end marker used in the call to MAKE-PARSER.
;;;
;;; MEH: To cater for proper lists, next-input should return a list such
;;;      that first is as CAR above and second is as CDR above.
;;;
;;;  The second argument to LALR-PARSER, PARSE-ERROR will be called
;;;  if the parse fails because the input is ill-formed.
;;;
;;; MEH: parse-error is given one parameter: the current category.
;;;
;;;  There is a sample at the end of this file.
;;;
;;; MEH: The original sample has been modified slightly. There is also
;;;      a second sample. It evaluates infix expressions and, with a
;;;      hack, caters for brakets too.

;;; DotLisp conversions
;;; , -> ~
;;; #' -> deleted
;;; eg #'(lambda
;;; let (()()) -> let ()
;;; let* -> lets
;;; cond (()()) -> cond ()()
;;; otherwise -> :else
;;; block inserted in cond
;;; Trace:* inserted, but commented out
;;; dotimes (i n) ... -> dotimes i n ...
;;; 1+ -> + 1
;;; &optional -> &opt
;;; dolist -> do-list
;;; try -> tryCat (just in case!)
;;; reduce ... :init -> :initial-value
;;; translateState adjusted to cater for DotLisp case
;;; find -> Find
;;;
;;; Proper List conversions
;;; get-assoc instead of cdr (assoc

;DotLisp Conversions

(when-not (.isDefined 'for-each-match)
 (load "extra.lisp")
)

(def-macro (Find i lst)
`(.MoveNext (find ~i ~lst))
)
(set some     any)
(set t        true)
(set null     nil?)
(set length   len)

(def (remove-if-not f lst)
 (into () (filter(fn(x)(to-bool(f x))) lst))
)

(def (delete-if f lst)
 (into () (filter (fn (x) (not (f x))) lst))
)

(def-macro (defmacro name args &rest body)
`(def-macro (~name ~@args)
 `(block
   ;(Trace:WriteLine ~~(.ToString name))
  ~(block
   ~@(if (stringp (first body))(rest body)body)
))))

(def-macro (defun name (&rest args) &rest body)
`(def (~name ~@args)
  ;(Trace:WriteLine ~(.ToString name))
~@(if (stringp (first body))(rest body)body)
))

(def (stringp str) (is? str String.))

(set car     first)
(set cdr     rest)
(set cadr    second)
(set unless  when-not)
(set =       ==)

(def (eq o1 o2)
 (if (nil? o1)
  (nil? o2)
  (if (nil? o2)
   false
   (_eq o1 o2)
)))
(when (is? eqv? Function.)
 (set
  builtin-eqv? eqv?
  eqv? eq
)) 
(def-binop (_eq (o1 Object.)(o2 Object.))
 (or (eql? o1 o2)
  (builtin-eqv? o1 o2)
))
(def-binop (_eq (o1 Cons.)(o2 Object.))
 false
)
(def-binop (_eq (c1 Cons.)(c2 Cons.))
 (or (eql? c1 c2)
  (and (eq (first c1) (first c2))
   (eq (rest c1) (rest c2))
)))
(def-binop (_eq (o1 Record.)(o2 Object.))
 false
)
(def-binop (_eq (r1 Record.)(r2 Record.))
 (or (eql? r1 r2)
  (_eq (SortedList. r1)(SortedList. r2))
))
(def-binop (_eq (o1 SortedList.)(o2 Object.))
 false
)
(def-binop (_eq (s1 SortedList.)(s2 SortedList.))
 (or (eql? s1 s2)
  (and (== s1.Count s2.Count)
   (every eq s1 s2)
)))
(def-binop (_eq (o1 DictionaryEntry.)(o2 Object.))
 false
)
(def-binop (_eq (d1 DictionaryEntry.)(d2 DictionaryEntry.))
 (or (eql? d1 d2)
  (and (eq d1.Key d2.Key)
   (eq d1.Value d2.Value)
)))


(def-macro (push item place)
 ;`(cond
 ;(not (list? ~place))
 ; (error "push: second argument not a list!")
 ;:else
  `(push! ~item ~place)
);)

(def (nset-difference x y)
 (cond
 (not (list? x))
  (error "nset-diff first arg not a list")
 (not (list? y))
  (error "nset-diff second arg not a list")
 (nil? y)
  x
 (nil? x)
  x
 :else
  (- x y)
))

(def (nunion set1 set2)
 (cond
 (not (list? set1))
  (error "nunion first arg not a list")
 (not (list? set2))
  (error "nunion second arg not a list")
 :else
  (union set1 set2)
))

(def (subsetp set1 set2)
 (cond
 (not (list? set1))
  (error "subsetp first arg not a list")
 (not (list? set2))
  (error "subsetp second arg not a list")
 :else
  (subset? set1 set2)
))

(def-macro (pop place)
 (let (p (gensym) q (gensym))
 `(lets
   (~p ~place
    ~q (if (list? ~p)
        (first ~p)
        (error "pop arg is not a list!")
   )   )
   (pop! ~place)
   ~q
)))

(def (caar lst)
 (let (f (first lst))
  (if f (first f)
   (error "caar argument does not contain a list")
)))

(def (cddr lst)
 (let (r (rest  lst))
  (if r (rest r)
   (error "cddr argument is not a list length 2 (or more)")
)))

;(def (get-assoc val lst)
; (let (r nil)
;  (for (l lst)
;   (not
;    (or (nil? l)
;     (when (eqv? val (first (first l)))
;      (set r (second (first l)))
;      true
;   )))
;   (next! l)
;  )
;  r
;))

(def-macro (funcall f &rest args)
`(~f ~@args))

(def nreverse reverse)
(def listp list?)
(def nthcdr nth-rest)

(set setq set)
(set setf set)

(def-macro (lambda (&rest args) &rest body)
`(fn ~args ~@body))

(def-macro (defconst name value &opt desc)
`(set ~name ~value))

(def-macro (defvar name &opt value desc)
 (if (missing? value)
 `(set ~name nil)
 `(set ~name ~value)
))

(def (subseq seq start &opt end)
 (butlast
  (nth-rest start seq)
  (if (missing? end)
   0
   (- (len seq) end)
)))

(set incf ++)

(def (replace-in-string string search replace)
 (string.Replace search replace))

(def print prns)
(def princ prs)

(def-macro (__format1 fmt args)
`(let
  (re (Regex. "(%{((%[^{}])|[^%]+)*%})|(%[^{}])|[^%]+")
   new-fmt  ""
   new-args nil
   n        0
  )
  (for-each-match arg re ~fmt
   (if (and (> arg.Value.Length 1)
            (== (0 arg.Value) (0 "%")))
    (let (c (arg.Value.Substring 1 1))
     (cond
    (== c "%")
     (set new-fmt (+ new-fmt (+ "\n")))
    (== c "{")
     (lets (sub-fmt (arg.Value.Substring 2 (- arg.Value.Length 4))
            sub-str (apply format* sub-fmt (first ~args))
           )
      (set new-fmt  (+ new-fmt (+ "{" n "}"))
           new-args (append new-args (list sub-str)))
      (next! ~args)
      (++ n)
     )
    (== c "a")
     (block
      (set new-fmt  (+ new-fmt (+ "{" n "}"))
           new-args (append new-args (list (str (first ~args)))))
      (next! ~args)
      (++ n)
     )
    :else
     (block
      (set new-fmt  (+ new-fmt (+ "{" n ":" c "}"))
           new-args (append new-args (list (first ~args))))
      (next! ~args)
      (++ n)
      )))
    (set new-fmt (+ new-fmt arg.Value))
  ))
  ;(Trace:WriteLine (+ new-fmt " " (str new-args)))
  (String:Format new-fmt (apply vector-of Object. (map->list (fn(a)(if(stringp a)a(str a)))new-args)))
))

(def (format fmt &rest args)
 (__format1 fmt args)
)
(def (format* fmt &rest args)
 (let (s "")
  (while args
   (+= s (__format1 fmt args))
  )
  s
))

; sort used only once to keep rules in no order
(def (sort lst index)
 (let
  (keys (.ToArray (into (ArrayList.) (map1 index lst)))
   lsta (.ToArray (into (ArrayList.) lst))
  )
  (Array:Sort keys lsta)
  (into () lsta)
))

(def (append-lists seq &key (init nil))
 (reduce append seq :init init)
)

(def-macro (mapcar f &rest seqs)
`(map->list ~f ~@seqs))

(def-macro (mapcan f &rest seqs)
`(append-lists (map ~f ~@seqs))
)

; NB record being defined at macro-expansion time!
(def-macro (defstruct name &rest fields)
 (when-not (symbol? name) (error "defstruct name must be a symbol"))
 (let (rtype (apply def-record name fields))
 `(block
   (def (~(intern (+ "make-" (name.ToString)))
     &key ~@(map->list (fn (f) (list f nil)) fields))
    (make-record ~rtype ~@(__enum-fields fields))
   )
 ~@(map->list
    (fn (field)
     (lets
      (get-name (+ (name.ToString) "-" (field.ToString))
       set-name (+ get-name "-setter")
       getter   (intern get-name)
       setter   (intern set-name)
      )      
     `(block
       (def (~getter struct)
        (~(__memberize field) struct)
       )
       (def (~setter struct value)
        (~(__memberize field) struct value)
       )
       (def-setter '~getter '~setter)
    )))
    fields
))))

(def (__enum-fields fields)
 (append-lists
  (map1
   (fn (field)
    (list (keyword field) field)
   )
   fields
)))

(def-macro (labels name-defs &rest body)
`(letfn ~(__label-process name-defs) ~@body)
)

(def (__label-process name-defs)
 (append-lists
  (map1 
   (fn (name-def)
   `((~(first name-def) ~@(second name-def))
     (block
      ;(Trace:WriteLine ~(+ "label:" (.ToString (first name-def))
      ; ; " " (str (rest (rest name-def)))
      ;))
    ~@(rest (rest name-def))
   )))
   name-defs
)))

; Need to cater for (return ...) in do-list.
; Only works because return is always the last statement in a loop.
(def-macro (do-list (var lst &rest result) &rest body)
 (let
  (return-result (gensym)
   return-called (gensym)
   current-list  (gensym)
  )
 `(let
   (~return-result nil
    ~return-called nil
    ~current-list  ~lst)
   (unless (list? ~current-list) (error "do-list 'list' isn't a list!"))
   (letfn
    ((return &opt value)
      (set
      ~return-result
         (if (missing? value) nil value)
      ~return-called true
    ) )
    ;(Trace:WriteLine "do-list")
    (until (or ~return-called (nil? ~current-list))
     (let (~var (first ~current-list))
      (next! ~current-list)
      ;(Trace:Write ".")
      ~@body
    ))
    ;(Trace:WriteLine "")
    (if ~return-called
     ~return-result
     (block
     ~@result)
)))))

; Need to cater for (return ...) in do.
; Only works because return is always the last statement in a loop.
(def-macro (do vars (test &rest result) &rest body)
 (let
  (return-result (gensym)
   return-called (gensym)
  )
  (if (nil? result)
  `(let
    (~return-result nil
     ~return-called nil
    )
    (letfn
     ((return &opt value)
       (set
       ~return-result
          (if (missing? value) nil value)
       ~return-called true
     ) )
     (for ~(__do-init vars) (not (or ~return-called ~test)) ~(__do-next vars)
      ;(Trace:Write ";")
      ~@body
     )
     (when ~return-called
     ~return-result
   )))
   (let (result-value (gensym))
   `(let
     (~return-result nil
      ~return-called nil
      ~result-value  nil
     )
     (letfn
      ((return &opt value)
        (set
        ~return-result
           (if (missing? value) nil value)
        ~return-called true
      ) )
      (for ~(__do-init vars)
           (not (or
            ~return-called
             (when ~test
              (set ~result-value (block ~@result))
              true
           )))
           ~(__do-next vars)
       ;(Trace:Write ":")
       ~@body
       )
      ;(Trace:WriteLine "")
      (if ~return-called
       ~return-result
       ~result-value
)))))))

(def (__do-init vars)
 (append-lists
  (map1
   (fn (var-init-next)
    (list (first var-init-next) (second var-init-next))
   )
   vars
)))

(def (__do-next vars)
 (append-lists
  (map1
   (fn (var-init-next)
    (when (rest (rest var-init-next))
     (list (first var-init-next) (third var-init-next))
   ))
   vars
  )
 :init
 '(set)
))
;;End of DotLisp Conversions

;;; Proper List Conversions

(defun get-assoc (val lst)
 (do-list (l lst)
  (when (eq val (first l))
   (return (second l))
)))

;;;End of Proper List Conversions

;;; ELisp conversions

(defmacro defconstant (name value &opt desc)
 (if desc
 `(defconst ~name ~value ~desc)
 `(defconst ~name ~value)))

(defmacro defparameter (name value &opt desc)
 (if desc
 `(defconst ~name ~value ~desc)
 `(defconst ~name ~value)))

(defmacro format-el (expression &rest values)
 (if (stringp expression)
  (let
    (exp
     (reduce (lambda (x y) (apply replace-in-string x y))
     '(("~D" "~d")("~" "%"))
     :init expression
   ))
  `(format ~exp ~@values)
  )
 `(format (reduce (lambda (x y)(apply replace-in-string x y))
   '(("~D" "~d")("~" "%"))
    :init ~expression
   )
  ~@values
)))

(defmacro cl-format (print-it expression &rest values)
 (if print-it
 `(princ (format-el ~expression ~@values))
        `(format-el ~expression ~@values)
))

(defconst NIL '())

;;; definitions of constants and global variables used
;;;
;;; MEH: gfirsts removed, never used.

(defconstant *TOPCAT* '$Start)
(defvar      *lalr-debug* NIL "Inserts debugging code into parser if non-NIL")

(defvar      *ENDMARKER*)
(defvar      glex)
(defvar      gstart)
(defvar      grules)
(defvar      gcats)
(defvar      gexpansions)
(defvar      gepsilons)
(defvar      gstarts)
(defvar      stateList '())

(defun make-parser (grammar lex endMarker)
 "Takes a grammar and produces the Lisp code for a parser for that grammar"
 (setq *ENDMARKER* endMarker)
 
 ;;;  cache some data that will be useful later
 (setq glex        lex)
 (setq gstart      (caar grammar))
 (setq grules      (let (i 0)
                     (mapcar (lambda (r) (transformRule r (incf i)))
                             grammar)))
 ;(setq gcats       (getallcats))
 (setq gcats       (GetAllCats))
 (setq gexpansions (mapcar (lambda (cat)
                                    ; cons->list for proper assoc list
                                    (list cat (compute-expansion cat)))
                                gcats))
 ;;; Check grammar, especially glex, which should be derivable
 ;;; from gexpansions as implied here
 (unless
  (subset? glex gcats)
  (error "Extraneous tokens in lex.")
 )
 (unless
  (congruent?
   (map->list first
    (filter (fn (x) (not (second x)))
     gexpansions
   ))
   glex
  )
  (error "Some lex tokens have rules!")
 )
 (setq gepsilons   (remove-if-not derivesEps gcats))
 (setq gstarts     (cons (list *ENDMARKER* (list *ENDMARKER*))
                         (mapcar (lambda (cat)
                                      ; MEH: cons->list for proper assoc list
                                      (list cat (FirstTerms (list cat))))
                                  gcats)))

 ;;; now actually build the parser
 (buildTable)
 (when (and *lalr-debug* (listp *lalr-debug*) (member 'print-table *lalr-debug*))
  (Print-Table stateList))
 (buildParser)
)

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;
;;;                    Rules and Grammars
;;;

(defstruct rule no mother daughters action)

(defun transformRule (rule no)
 (make-rule :no        no
            :mother    (first rule)
            :daughters (butlast (cddr rule))
            :action    (car (last rule))))

(defun compute-expansion (cat)
 (remove-if-not (lambda (rule)
                         (eq (rule-mother rule) cat))
                     grules))
    
(defmacro expand (cat)
`(get-assoc  ~cat gexpansions))
    
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;
;;;                    Properties of grammars
    
(defun GetAllCats ()
 (labels
  ((tryCat (dejaVu cat)
    (if (Find cat dejaVu)
     dejaVu
     (tryRules (cons cat dejaVu) (compute-expansion cat))
   ))
   (tryRules (dejaVu rules)
    (if rules
     (tryRules (tryCats dejaVu (rule-daughters (car rules))) (cdr rules))
     dejaVu
   ))
   (tryCats (dejaVu cats)
    (if cats
     (tryCats (tryCat dejaVu (car cats)) (cdr cats))
     dejaVu
  )))
  (tryCat '() gstart)
))

(defun derivesEps (c)
 "t if c can be rewritten as the null string"
 (labels
 ((tryCat (dejaVu cat)
   (unless (Find cat dejaVu)
    (some (lambda (r) 
      (every (lambda (c1) (tryCat (cons cat dejaVu) c1))
       (rule-daughters r)))
     (expand cat)))))
  (tryCat '() c)))

(defun derivesEpsilon (c)
  "looks up the cache to see if c derives the null string"
  (member c gepsilons))

(defun FirstTerms (catList)
 "the leading terminals of an expansion of catList"
 (labels
  ((firstDs (cats)
    (if cats
     (if (derivesEpsilon (car cats))
      (cons (car cats) (firstDs (cdr cats)))
      (list (car cats))
   )))
   (tryCat (dejaVu cat)
    (if (member cat dejaVu)
     dejaVu
     (tryList (cons cat dejaVu) 
      (mapcan (lambda (r) (firstDs (rule-daughters r)))
       (expand cat)
   ))))
   (tryList (dejaVu cats)
    (if cats
     (tryList (tryCat dejaVu (car cats)) (cdr cats))
     dejaVu
  )))
  (remove-if-not (lambda (term)
    (or (eq *ENDMARKER* term)
        (Find term glex))) 
   (tryList '() (firstDs catList))
)))
    
(defun FirstTerminals (catList)
 (if catList
  (if (derivesEpsilon (first catList))
   (union (get-assoc  (first catList) gstarts)
          (FirstTerminals (rest catList)))
   (get-assoc  (first catList) gstarts)
  )
 '()
))

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;
;;;                  LALR(1) parsing table constructor
;;;

(defstruct item rule pos la)

(defmacro item-daughters (i) `(rule-daughters (item-rule ~i)))

(defmacro item-right (i) `(nthcdr (item-pos ~i) (item-daughters ~i)))

(defmacro item-equal (i1 i2)
  `(and (eq (item-rule ~i1) (item-rule ~i2))
        (=  (item-pos  ~i1) (item-pos  ~i2))
        (eq (item-la   ~i1) (item-la   ~i2))))

(defmacro item-core-equal (c1 c2)
  "T if the cores of c1 and c2 are equal"
  `(and (eq (item-rule ~c1) (item-rule ~c2))
        (=  (item-pos  ~c1) (item-pos  ~c2))))

(defun close-items (items)    
 "computes the closure of a set of items"
 (do ((toDo items))
     ((null toDo) items)
  (let (i (pop toDo))
   (when (item-right i)
    (do-list (la (FirstTerminals (append (rest (item-right i)) (list (item-la i)))))
     ;(Trace:WriteLine (+ "la: "(str la)))
     (do-list (r (expand (first (item-right i))))
      ;(Trace:WriteLine (+ " r: " (str r)))
      (unless (do-list (i items)
               (if (and (eq (item-rule i) r)
                        (=  (item-pos  i) 0)
                        (eq (item-la   i) la))
                (return t)))
       (let (new (make-item :rule r :pos 0 :la la))
        ;(Trace:WriteLine (str new))
        (push new items)
        (push new toDo)
))))))))

(defun shift-items (items cat)
 "shifts a set of items over cat"
 ;(prns "Shift over: " (str cat))
 ;(mapcat! (fn(i)(prn (first(item-right i)) i)) items)
 (labels
  ((shift-item (item)
    (if (eq (first (item-right item)) cat)
     (make-item :rule (item-rule item)
                :pos  (+ 1 (item-pos item))
                :la   (item-la item)
  ))))
  (let (new-items '())
   (do-list (i items)
    (let (n (shift-item i))
     (if n
      (push n new-items)
   )))
   ;(Trace:WriteLine (+ "S-I : " (str new-items)))
   ;(prns "Shifted:")
   ;(mapcat! (fn(i)(prn (first(item-right i)) i)) new-items)
   new-items
)))

(defun items-right (items)
 "returns the set of categories appearing to the right of the dot"
 (let (right '())
  (do-list (i items)
   (let (d (first (item-right i)))
    (if (and d (not (Find d right)))
     (push d right))))
  right))

;emacs lisp:
;(defun keyfn (i) (rule-no (item-rule i)))

(defun compact-items (items)
 "collapses items with the same core to compact items"
 ;(mapcat! prn items)(prns":")
 (let (soFar '())
  (do-list (i items)
   (let
    (ci
     (do-list (s soFar)
      (if (item-core-equal s i)
       (return s)
    )))
    ;(prn (to-bool ci))
    (if ci
     (push (item-la i) (item-la ci))
     (push (make-item :rule (item-rule i)
                      :pos  (item-pos  i)
                      :la   (list (item-la i)))
      soFar
  ))))
;CL:
;  (sort soFar #'< 
;        :key #'(lambda (i) (rule-no (item-rule i))))
;emacs lisp:
;  (sort soFar #'(lambda (i j) (< (keyfn i)(keyfn j))))
;DotLisp:
  ;(mapcat! prn soFar)(prns".")
  (set soFar
   (sort soFar (lambda (i) (rule-no (item-rule i))))
  )
  ;(mapcat! prn soFar)(prn)
  ;(Trace:WriteLine (+ "C-Is: " (str soFar)))
  soFar
))

(defmacro expand-citems (citems)
 "expands a list of compact items into items"
`(let (items '())
  (do-list (ci ~citems)
   (do-list (la (item-la ci))
    (push (make-item :rule (item-rule ci)
                     :pos  (item-pos  ci)
                     :la   la)
     items
  )))
  items
))

(defun subsumes-citems (ci1s ci2s)
 "T if the sorted set of items ci2s subsumes the sorted set ci1s"
; (prn ci1s)(prn ci2s)(let (r
 (and (= (length ci1s) (length ci2s))
      (every (lambda (ci1 ci2)
                  (and (item-core-equal ci1 ci2)
                       (subsetp (item-la ci1) (item-la ci2))
               )  )
              ci1s ci2s
))    )
;(prn r)
;r))

(defun merge-citems (ci1s ci2s)
 "Adds the last of ci1s to ci2s.  ci2s should subsume ci1s"
 ;(Trace:Write (String:Format "   {0}\n   {1}\n-->" (str ci1s) (str ci2s)))
 (mapcar (lambda (ci1 ci2)
              (setf (item-la ci2) (nunion (item-la ci1) (item-la ci2))))
          ci1s ci2s)
 ;(Trace:WriteLine (str ci2s))
 ci2s
)

;;;  The actual table construction functions

(defstruct state name citems shifts conflict)
(defstruct shift cat where)

(defparameter nextStateNo -1)

;(defun lookup (citems)
;  "finds a state with the same core items as citems if it exits"
;  (find-if (lambda (state)
;               (and (= (length citems) (length (state-citems state)))
;                    (every (lambda (ci1 ci2)
;                               (item-core-equal ci1 ci2))
;                            citems (state-citems state))
;                    ))
;           stateList))

(defun lookup (citems)
 "finds a state with the same core items as citems if it exits"
 (do-list (state stateList)
  (if (and (= (length citems)
              (length (state-citems state)))
           (do ((ci1s citems               (cdr ci1s))
                (ci2s (state-citems state) (cdr ci2s)))
                ((null ci1s) t)
               (unless (item-core-equal (car ci1s) (car ci2s))
                 (return false) ; MEH: was nil -- false confirms return only going up one level.
      )    )   )
   (return state)
)))

(defun addState (citems)
  "creates a new state and adds it to the state list"
  (let (newState 
 ;        (make-state :name (intern    (format nil "STATE-~D" (incf nextStateNo)))
          (make-state :name (intern (cl-format nil "STATE-~D" (incf nextStateNo)))
                      :citems citems))
    (push newState stateList)
    newState))
    
(defun getStateName (items)
 "returns the state name for this set of items"
 (lets
  (citems (compact-items items)
   state  (lookup citems)
  )
  ;(Trace:WriteLine (+ "citems:" (str citems)))
  ;(if state
  ; (prns "Existing state:"(state-name state))
  ; (prns "NEW STATE!"))
  ;(if citems
  ; (mapcat! prn citems)
  ; (prns "No items!"))

  (cond
  (eql? state false)
   (error "do return returns through outer loop!")
  (null state)
   (block
    (setq state (addState citems))
    ;(Trace:WriteLine (+ "New state: " (str state)))
    (buildState state items)
   )
  (subsumes-citems citems (state-citems state))
   (;(Trace:WriteLine (+ "Subsumed: " (str state)))
   )
  :else
   (block
    (merge-citems citems (state-citems state))
    ;(Trace:WriteLine (+ "Follow (from) state: " (str state)))
    (followState items)
  ))
  (state-name state)
))

(defun buildState (state items)
 "creates the states that this state can goto"

 ;(prns "State: " (state-name state))
 ;(if (state-shifts state)
 ; (mapcat! prn (state-shifts state))
 ; (prns "None."))

 (let (closure (close-items items))

  ;(prns "Closure on: " (state-name state))
  ;(if closure
  ; (mapcat! prn closure)
  ; (prns "None."))

  (do-list (cat (items-right closure))
   (push (make-shift :cat   cat
                     :where (getStateName (shift-items closure cat)))
         (state-shifts state)
 )))

 ;(prns "State now: " (state-name state))
 ;(if (state-shifts state)
 ; (mapcat! prn (state-shifts state))
 ; (prns "None."))

)
    
(defun followState (items)
 "percolates look-ahead onto descendant states of this state"
 (let (closure (close-items items))
  (do-list (cat (items-right closure))
   (getStateName (shift-items closure cat))
)))

(defun buildTable ()
 "Actually builds the table"
 (setq stateList  '())
 (setq nextStateNo -1)
 (getStateName (list (make-item :rule (make-rule :no 0
                                                 :mother *TOPCAT*
                                                 :daughters (list gstart))
                                :pos 0
                                :la  *ENDMARKER*)))
 (setq stateList (nreverse stateList))
)
  
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;
;;;                  LALR(1) parsing table printer
;;;

(defun Print-Table (stateList)
 "Prints the state table"
 (do-list (state stateList)
 ;   (format t "~%~%~a:" (state-name state))
  (cl-format t "~%~%~a:" (state-name state))
  (do-list (citem (state-citems state))
 ;    (format t "~%  ~a -->~{ ~a~} .~{ ~a~}, ~{~a ~}"
   (cl-format t "~%  ~a -->~{ ~a~} .~{ ~a~}, ~{~a ~}"
              (rule-mother (item-rule citem))
              (subseq (rule-daughters (item-rule citem)) 0 (item-pos citem))
              (subseq (rule-daughters (item-rule citem)) (item-pos citem)  )
              (item-la citem)
  ))
  (do-list (shift (state-shifts state))
 ;    (format t "~%    On ~a shift ~a" (shift-cat shift) (shift-where shift)))
   (cl-format t "~%    On ~a shift ~a" (shift-cat shift) (shift-where shift)))
  (do-list (redux  (compact-items 
                  (delete-if (lambda (i) (item-right i))
                   (close-items (expand-citems (state-citems state)))
          )      ))
 ;    (format t "~%    On~{ ~a~} reduce~{ ~a~} --> ~a"
   (cl-format t "~%    On~{ ~a~} reduce~{ ~a~} --> ~a"
              (item-la redux )
              (rule-daughters (item-rule redux ))
              (rule-mother    (item-rule redux ))
 )))
 ;  (format t "~%"))
 (cl-format t "~%")
)

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;
;;;                  LALR(1) parser constructor
;;;

(defun translateState (state)
 "translates a state into lisp code that could appear in a labels form"
 (let
  (reduces
   (compact-items 
    (delete-if (lambda (i) (item-right i))
     (close-items (expand-citems (state-citems state)))
   ))
   symbolsSoFar '()
  )         ; to ensure that a symbol never occurs twice
  (labels
   ((translateShift (shift)
     (push (shift-cat shift) symbolsSoFar)
    `(~(shift-cat shift)
      (block
     ~@(when *lalr-debug*
       `((when *lalr-debug*
 ;        (princ    ~(format nil "Shift ~a to ~a~%" 
          (princ ~(cl-format nil "Shift ~a to ~a~%" 
            (shift-cat shift)
            (shift-where shift)
       )))))
       (shift-from ~(state-name state))
       (~(shift-where shift))
    )))
    (translateReduce (item)
     (unless (list? (item-la item)) (error "translateReduce look ahead not a list!"))
     (when (intersection (item-la item) symbolsSoFar)
 ;       (format t "Warning, Not LALR(1)!!: ~a, ~a --> ~{~a ~}~%"
      (cl-format t "Warning, Not LALR(1)!!: ~a, ~a --> ~{~a ~}~%"
       (state-name state) 
       (rule-mother (item-rule item))
       (rule-daughters (item-rule item))
      )
      (setf (item-la item)
       (nset-difference (item-la item) symbolsSoFar)
     ))
     (do-list (la (item-la item))
      (push la symbolsSoFar)
     )
    `(~(item-la item)
      (block
     ~@(when *lalr-debug*
       `((when *lalr-debug*
 ;           (princ ~(format nil "Reduce ~{~a ~} --> ~a~%"
          (princ ~(cl-format nil "Reduce ~{~a ~} --> ~a~%"
            (rule-daughters (item-rule item))
            (rule-mother (item-rule item))
       )))))
       (reduce-cat
     ~(state-name state)
      '~(rule-mother (item-rule item))
       ~(item-pos item)
       ~(rule-action (item-rule item))
   )))))
  `(~(state-name state) ()
    (case (input-peek)
     ; MEH: DotLisp case: mapcar -> mapcan
   ~@(mapcan translateShift (or (state-shifts state)
      ;(Trace:WriteLine (+ "state-shifts NIL for " (str(state-name state))))
     ))
     ; MEH: DotLisp case: mapcar -> mapcan
   ~@(mapcan translateReduce (or reduces
      ;(Trace:WriteLine (+ "reduces NIL for " (str(state-name state))
      ;      " (citems: " (str (state-citems state)) ")")
     ));)
    :else (funcall parse-error (input-peek))
)))))

;;;  next-input performs lexical analysis.  It must return a cons cell.
;;;  its first holds the category, its second the value.

(defun buildParser ()
 "returns an lalr(1) parser.  next-input must return 2 values!"
`(defun lalr-parser (next-input parse-error)
  (let (cat-la      '() ; category lookahead
        val-la      '() ; value lookahead
        val-stack   '() ; value stack
        state-stack '()); state stack
   (labels
    ((input-peek ()
      (unless cat-la
       (let (new (funcall next-input))
        ; MEH: car & cdr -> first & second for proper list
        (setq cat-la (list (first  new)))
        (setq val-la (list (second new)))
      ))
      (first cat-la)
     )
     (shift-from (name)
      (push name state-stack)
      (pop cat-la)
      (push (pop val-la) val-stack)
     )
     (reduce-cat (name cat ndaughters action)
      (if (eq cat '~*TOPCAT*)
       (pop val-stack)
       (let (daughter-values '()
             state           name)
        (dotimes i ndaughters
         (push (pop val-stack) daughter-values)
         (setq state (pop state-stack))
        )
        (push cat cat-la)
        (push (apply action daughter-values) val-la)
        (funcall state)
     )))
   ~@(mapcar translateState stateList)
    )
    (~(state-name (first stateList)))
))))

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;
;;;                   Test grammar and lexical analyser
;;;

;;;  A Test grammar

; MEH: return proper lists, and use full names
(defparameter grammar '((s  --> np  vp    (lambda (np  vp) (list 'Sentence   (mapcan second (list np  vp)) np  vp)))
                        (np --> det n     (lambda (det n)  (list 'NounPhrase (mapcan second (list det n )) det n )))
                        (np -->           (lambda ()      '(NounPhrase       nil                                )))
                        (vp --> v   np    (lambda (v   np) (list 'VerbPhrase (mapcan second (list v   np)) v   np)))
                        (vp --> v   s     (lambda (v   s)  (list 'VerbPhrase (mapcan second (list v   s )) v   s )))))

(defparameter lexforms '(det n v))

;;; (set *lalr-debug* '(print-table))
;;; (make-parser grammar lexforms '$) will generate the parser.
;;; After compiling that code, (parse <list-of-words>) invokes the parser.  E.g.
;;;
;;; ? (parse '(the man thinks the woman hates the dog $))
;;; (S (NP (DET THE) (N MAN)) (VP (V THINKS) (S (NP (DET THE) (N WOMAN)) (VP (V HATES) (NP (DET THE) (N DOG))))))
;;; See p-test below...

; proper list assoc
(defparameter lexicon '((the    det)
                        (man    n)
                        (woman  n)
                        (cat    n)
                        (dog    n)
                        (loves  v)
                        (thinks v)
                        (hates  v)
                        ($      $)))

(defun parse (words)
  (labels ((lookupw (word)
                   (get-assoc  word lexicon))
           (next-input ()
                       (lets (word (pop words)
                              cat  (lookupw word))
                         ; cons -> list for proper list
                         (list cat           ; category
                          (list (case cat n 'Noun v 'Verb det 'DefiniteArticle :else 'Error) (list word) word)))) ; value
           (parse-error (current)
 ;                     (format nil "Error before ~a" words)))
                        (cl-format nil "Error before ~a~%Current: ~a" words current)
          ))
    (lalr-parser next-input parse-error)))
                           
;;; *EOF*
(def (p-test)
 (let(tlst '(the man thinks the woman hates the dog $))
  (set *lalr-debug* '(print-table))
  (eval (make-parser grammar lexforms '$))
  (prns "Demo:" (str tlst))
  (pp (parse tlst))
))
(def (calc-test)
(set grammar '((Expr  --> Expr PlusMinus Term    (fn (x o y) (o x y)))
               (Expr  --> Term                   (fn (x)     x      ))
               (Term  --> Term MulDiv Factor     (fn (x o y) (o x y)))
               (Term  --> Factor                 (fn (x)     x      ))
               (Factor --> value                 (fn (x)     x      ))))

(set lexforms '(value PlusMinus MulDiv))

(set lexicon  '((+    PlusMinus)
                (-    PlusMinus)
                (*    MulDiv   )
                (/    MulDiv   )))
(eval (make-parser grammar lexforms '$))
(defun parse (words)
  (letfn ((lookupw word)
                   (or (get-assoc  word lexicon) 'value)
           (next-input)
                       (lets (word (pop words)
                              cat  (if word
                                    (if (cons? word)
                                     (block
                                      (set word (parse word))
                                      'value
                                     )
                                     (lookupw word)
                                    )
                                    '$
                                   ))
                         ; cons -> list for proper list
                         (list cat           ; category
                          (eval word))) ; value
           (parse-error current)
 ;                         (format nil "Error before ~a" words)))
                        (cl-format nil "Error before ~a~%Current: ~a" words current)
         )
   (lalr-parser next-input parse-error)))

(prns "Demo: (parse '(3 * (5 + 2) + (10 - 4 * 6 / 12)))\n" (parse '(3 * (5 + 2) + (10 - 4 * 6 / 12))))
)