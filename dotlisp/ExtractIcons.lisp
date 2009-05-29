(set dbgex nil)

;Icon extract example:

(load (Path:Combine AppDomain:CurrentDomain.BaseDirectory "extra.lisp")) ; for + on Enums, Windows.Forms and System.Drawing.

(PInvoke-declare ExtractIconEx :Lib "shell32.dll" :Args (vector-of Type. String. Int32. (ref (arr IntPtr.)) (ref (arr IntPtr.)) Int32. ) :Return-Type Int32.)
(PInvoke-declare ExtractIcon   :Lib "shell32.dll" :Args (vector-of Type. IntPtr. String. Int32. ) :Return-Type IntPtr.)

(def (get-icon-count filename)
 (ExtractIconEx filename -1 nil nil 0))

(def (get-icon filename index)
 (Icon:FromHandle (ExtractIcon IntPtr:Zero filename index)))

(def (or-cmp f args)
 (let (r 0)
  (for (lst args) (and lst (== 0 (set r (f (first lst))))) (next! lst))
  r))

(def (display-icons folder-path ico-files)
 (try
  (with-dispose
   (frmFrm (Form.)
    lvwLv    (ListView.)
    imlImages(ImageList.)
    imlSmalls(ImageList.)
    mnuFileExit  (MenuItem. "Exit")
    mnuFile (MenuItem. "File" [mnuFileExit])
    mnuViewDetails  (MenuItem. "Details")
    mnuViewIcons (MenuItem. "Icons")
    mnuViewSortByFile  (MenuItem. "By File")
    mnuViewSortByIndex (MenuItem. "By Index")
    mnuViewSortByException (MenuItem. "By Exception")
    mnuViewSort (MenuItem. "Sort" [mnuViewSortByFile mnuViewSortByIndex mnuViewSortByException])
    mnuView (MenuItem. "View" [mnuViewIcons mnuViewDetails mnuViewSort])
    mnuMain (MainMenu. [mnuFile mnuView])
   )
   (letfn (
    (sort-by column)
     (let (ordering (case column
        0 #| Name      |# (list 0 1 2)
        1 #| Index     |# (list 1 0 2)
        2 #| Exception |# (list 2 0 1)))
      (mnuViewSortByFile.Checked      (== column 0))
      (mnuViewSortByIndex.Checked     (== column 1))
      (mnuViewSortByException.Checked (== column 2))
      (set lvwLv.ListViewItemSorter (make-interface .IComparer:Compare (x y) ordering
        (try (or-cmp (fn (c)
           (if (== c 1)
            (- (Int32:Parse (.Text (c x.SubItems))) (Int32:Parse (.Text (c y.SubItems))))
            (String:Compare (.Text (c x.SubItems)) (.Text (c y.SubItems)))
          ))
          ordering)
         :catch(block(dex ex)0)
         ;:finally 0
     ))))
    (switch-view view)
     (let (details (== view View:Details))
      (unless (== lvwLv.View view)
       (lvwLv.View view))
      (when (and details (not smalls-done))
       (let (
         large-images imlImages.Images
         small-images imlSmalls.Images
        )
        ;(when-not smalls-done
        (try
         (for-each i large-images
          (small-images.Add i)
         )
        :catch (prns (sex ex))
        )
        (set smalls-done true)
      ));)
      (mnuViewDetails.Checked details)
      (mnuViewIcons.Checked (not details))
    ))

    (frmFrm.add_Load (make-delegate System.EventHandler. (s e)
      (frmFrm.Text (+ "Icons from " folder-path))
      (frmFrm.Menu mnuMain)
      (lvwLv.Clear)
      (lvwLv.Columns.Add "Name"       122 HorizontalAlignment:Left)
      (lvwLv.Columns.Add "Index"       48 HorizontalAlignment:Right)
      (lvwLv.Columns.Add "Exception"  512 HorizontalAlignment:Left)
      (lvwLv.BeginUpdate)
      (imlImages.ImageSize SystemInformation:IconSize)
      (lvwLv.LargeImageList imlImages)
      (lvwLv.SmallImageList imlSmalls)
      (set smalls-done false)
      (let
       (inum 0
        large-images imlImages.Images
        ;small-images imlSmalls.Images
        dir-info (DirectoryInfo. folder-path))
       (for-each f
        (apply concat
         (map (fn (ft)
           (try
            (dir-info.GetFiles ft)
           :catch
            (block
             (prns ft)
             (prns (sex ex))
             nil)))
          ico-files))
        (let (c (or (try (get-icon-count f.FullName)
                    :catch (block (prns f.FullName) (prns (sex ex)) 0))
                    0))
         (dotimes i c
          (let (errmsg nil ico nil)
           (try (set ico (get-icon f.FullName i))
           :catch (set errmsg (sex ex)))
           (if (or (nil? ico) errmsg)
            (let (entry (.SubItems (lvwLv.Items.Add f.Name)))
             (entry.Add (str i))
             (entry.Add (or errmsg "No icon loaded")))
            (let (entry (.SubItems (lvwLv.Items.Add f.Name inum)))
             (entry.Add (str i))
             (try (block (large-images.Add ico)
               (++ inum)
               ;(small-images.Add ico)
              )
             :catch (prns (sex ex)))
             (entry.Add "") ; no exception (pun not intended, but accurate)
             (.IDisposable:Dispose ico)
      )))))))
      (lvwLv.EndUpdate)
      (sort-by 0)
      (switch-view View:LargeIcon)
      (frmFrm.BringToFront)
    ))
    (lvwLv.Bounds (Rectangle. 10 10 (- frmFrm.ClientSize.Width 20) (- frmFrm.ClientSize.Height 20)))
    (lvwLv.add_ColumnClick (make-delegate ColumnClickEventHandler. (s e)
      (sort-by e.Column)
    ))
    (frmFrm.Controls.AddRange (vector-of Control. lvwLv))

    (frmFrm.add_Resize (make-delegate System.EventHandler. (s e)
      (lvwLv.Bounds (Rectangle. 10 10 (- frmFrm.ClientSize.Width 20) (- frmFrm.ClientSize.Height 20)))
    ))
    (frmFrm.add_DoubleClick (make-delegate System.EventHandler. (s e)
      (if (== lvwLv.View View:Details)
       (switch-view View:LargeIcon)
       (switch-view View:Details)
    )))
    (mnuFileExit.add_Click (make-delegate System.EventHandler. (s e)
      (Application:Exit)
    ))
    (mnuViewDetails.add_Click (make-delegate System.EventHandler. (s e)
      (switch-view View:Details)
    ))
    (mnuViewIcons.add_Click (make-delegate System.EventHandler. (s e)
      (switch-view View:LargeIcon)
    ))
    (let (order-by (make-delegate System.EventHandler. (s e)
       (sort-by (cond
         (eql? s mnuViewSortByFile) 0
         (eql? s mnuViewSortByIndex) 1
         (eql? s mnuViewSortByException) 2
     ))))
     (mnuViewSortByFile.add_Click order-by)
     (mnuViewSortByIndex.add_Click order-by)
     (mnuViewSortByException.add_Click order-by)
    )
    (frmFrm.ShowInTaskbar true)
    (frmFrm.TopLevel true)
    (frmFrm.Show)
    (frmFrm.Activate)
    ;(frmFrm.ShowInTaskbar true)
    (Application:Run frmFrm)
  ))
 :catch (block (set dbgex ex) (dex ex))))

(lets
 (cmd-array (Environment:GetCommandLineArgs)
  last-arg  ((- cmd-array.Length 1) cmd-array))

 ; Check for bug: last-arg = C:\" for drives!
 (when (== (last-arg.Substring (- last-arg.Length 1)) "\"")
  (set last-arg (last-arg.Substring 0 (- last-arg.Length 1))))

 (display-icons last-arg
  '("*.ico" "*.exe" "*.dll" "*.ocx" "*.com" "*.sys" "*.bin" "*.drv" "*.lib" "*.cur" "*.cmp")
))

(when dbgex (throw dbgex))

; *MUST* exit because further parsing of the command line by dotlisp.exe will result in load errors...
(Application:Exit)
(Environment:Exit 0)
