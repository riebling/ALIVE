(def (header-regex header)
 (Regex. (+ "^" header ":\\W*((.|\n\\W)*)\\W*$")
  RegexOptions:Multiline))

(def-macro (for-each-email-header-line hdr filename &rest body)
 (with-gensyms (txt f)
 `(with-dispose (~f
   (File:OpenText ~filename))
   (let (~txt
     (enum-stream ~f))
    (while (and (.MoveNext ~txt)
      (positive? (.Length (.Current ~txt))))
     (let (~hdr
       (.Current ~txt))
      ~@body))))))

(def (enum-email-header-lines filename)
 (make-enum
  (f   (File:OpenText filename)
   txt (enum-stream f))
  txt.Current
  (if (and txt.MoveNext
    (positive? txt.Current.Length))
   true
   (block
    f.Close
    (.IDisposable:Dispose f)
    (set f   nil
         txt nil)
    false))))

(def (enum-email-headers filename)
 (make-enum
  (txt (enum-email-header-lines filename)
   hdr nil
   nxt (and txt.MoveNext txt.Current))
  hdr
  (if (set hdr nxt) 
   (block
    (while
     (block (set nxt nil)
      (and txt.MoveNext
       (Char:IsWhiteSpace (0 (set nxt txt.Current)))))
     (+= hdr (+ "\r\n" nxt)))
    true)
   (block
    (set txt nil)
    false))))

(def (enum-bad-mail header)
 (let (dh (header-regex "Date")
   xr (header-regex header))
  (map (fn (f)
    (let (dt nil g nil)
     (for-each h (enum-email-headers f.FullName)
      (cond (dh.IsMatch h)
       (set dt h)
       (xr.IsMatch h)
       (set g (append g (list h)))))
     (apply list dt (or g (list f.Name)))))
   (.GetFiles (DirectoryInfo. "E:\\badmail")))))

(def (enum-virus-mail header)
 (let (dh (header-regex "Date")
   xr (header-regex header))
  (map (fn (f)
    (let (dt nil g nil)
     (for-each h (enum-email-headers f.FullName)
      (cond (dh.IsMatch h)
       (set dt h)
       (xr.IsMatch h)
       (set g (append g (list h)))))
     (apply list dt (or g (list f.Name)))))
   (apply concat (map->list (fn (dir) 
      (.GetFiles (DirectoryInfo. (+ "\\\\smsq.hurd.au\\Download\\Virii\\Until 0412" dir))))
     '("111607" "121120"))))))

(def (get-date-from-header hdr)
 (lets
  (c (hdr.IndexOf ",")
   a (hdr.IndexOf "+")
   m (hdr.IndexOf "-")
   g (max (hdr.IndexOf "GMT") (hdr.IndexOf "UTC"))
   p (max a m g)
   d (DateTime:Parse (if (== p -1) (hdr.Substring (+ c 2))
       (hdr.Substring (+ c 2) (- p c 2))))
   tz (when (!= g p)(hdr.Substring (+ p 1) 4))
   ht (if (== g p)0(Int32:Parse (tz.Substring 0 2)))
   mt (if (== g p)0(Int32:Parse (tz.Substring 2))))
  (.AddMinutes (d.AddHours (if(== a p)(- 0 ht)ht)) (if(== a p)(- 0 mt)mt))))

#|

(into (arr Cons.)
 (filter rest (map (fn (dr)
    (into () (concat (list (first dr))
      (filter (fn (r)
        (positive? (r.IndexOf "from")))
       (rest dr)))))
   (enum-virus-mail "Received"))))

(Array:Sort $ (make-interface .IComparer:Compare (a b) nil
  (DateTime:Compare
   (get-date-from-header (first a))
   (get-date-from-header (first b)))))

(mapcat! (fn (dh)
  (prns (first dh))
  (mapcat! prns (rest dh))
  (prn))
 (filter (fn (dr)
   (not (any (fn (r)
      (positive? (r.IndexOf "iprimus")))
     (rest dr))))
  $$))

|#
