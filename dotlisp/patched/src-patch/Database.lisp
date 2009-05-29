(load-assembly "System.Data")

; prn System.Data.Common.DbDataRecord something like record.
(def-method (str (r System.Data.Common.DbDataRecord.))
 (apply +
  (str (type-of r)) "\r\n"
  (mapcat! (fn (k)
    (list (.System.Data.Common.DbDataRecord:GetName r k) " " (str (k r)) "\r\n"))
   (range 0 (.System.Data.Common.DbDataRecord:FieldCount r)))))

; (into :sql list) produces "( 'x', 'y', ...)"
(def-method (into (sql :sql) seq &key (quote-char "'"))
 (+ "("
  (or (reduce (fn (r s)
     (+ r ", " s))
    (map (fn (v)
      (let (s (if v v.ToString
         ""))
       (if quote-char
        (quote-string s :quote-char quote-char)
        s)))
     seq)) "")
  ")"))

(def (quote-string s &key (quote-char "'"))
        (+ quote-char (s.Replace quote-char (+ quote-char quote-char)) quote-char))

(def (sql sql-command &key (returns :value) (records false) (column false) connect-string time-out)
 (let
  (result nil
   connect-str (cond
   (or (missing? connect-string)(== connect-string.ToString.ToLower "netsdk"))
    "DATABASE=PUBS;SERVER=(local)\\NetSDK;UID=SA;PWD=sapwd"
   (== connect-string.ToString.ToLower "local")
    "DATABASE=PUBS;SERVER=(local);UID=SA;PWD=sapwd"
   :else
    connect-string
  ))
  (cond
   records
    (set returns :records)
   column
    (set returns :column)
  )
  (with-dispose
   (db  (SqlConnection. connect-str)
    cmd (db.CreateCommand)
   )
   (unless (missing? time-out)
    (set cmd.CommandTimeout time-out))
   (cmd.CommandText sql-command)
   (db.Open)
   (set result (case returns
     :records
      (into (ArrayList.) (cmd.ExecuteReader))
     :column
      (map->list 0 (cmd.ExecuteReader))
     :else
      (cmd.ExecuteScalar)))
   (db.Close)
  )
  result
))
