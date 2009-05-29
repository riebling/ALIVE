Option Explicit On
Option Strict On

Imports System
Imports System.Threading
Imports DotLisp
Imports DotLispREPL.ConsoleCtrl
Imports DotLispREPL.ConsoleCtrl.ConsoleEvent

Namespace DotLispREPL

 Class ConsoleCtrl
  Implements IDisposable

  Public Enum ConsoleEvent As Integer ' DWORD
   None          = -1 ' MEH defined...
   CTRL_C        =  0 ' From wincom.h
   CTRL_BREAK    =  1
   CTRL_CLOSE    =  2
   CTRL_LOGOFF   =  5 ' Not guarenteed which user!
   CTRL_SHUTDOWN =  6 ' Services only
  End Enum

  ' Handler to be called when a console event occurs.
  Private Delegate Function HandlerRoutine(ByVal TheEvent As ConsoleEvent) As Int32 ' BOOL

  Public Class ConsoleEventArgs
   Inherits EventArgs
   Public TheEvent As ConsoleEvent
   Public Handled  As Boolean
  End Class

  ' Event fired when a console event occurs
  Public Event ControlEvent(ByVal ConsoleEvent As ConsoleEventArgs)

  Private EventHandler As HandlerRoutine

  Public Sub New()

   ' save this to a private var so the GC doesn't collect it...
   EventHandler = AddressOf Handler
   SetConsoleCtrlHandler(EventHandler, True)
  End Sub

  Protected Overrides Sub Finalize()
   Dispose(False)
   MyBase.Finalize()
  End Sub

  Public Sub Dispose() Implements IDisposable.Dispose
   Dispose(True)
  End Sub

  Public Sub Dispose(ByVal Disposing As Boolean)

   If Not (EventHandler Is Nothing) Then
    SetConsoleCtrlHandler(EventHandler, False)
    EventHandler = Nothing
   End If
  End Sub

  Private Function Handler(ByVal TheEvent As ConsoleEvent) As Int32
   Dim Args As New ConsoleEventArgs()
   If Not (EventHandler Is Nothing) Then
        Args.TheEvent = TheEvent
    RaiseEvent ControlEvent(Args)
   End If
   Return Convert.ToInt32(Args.Handled)
  End Function

  Private Declare Function SetConsoleCtrlHandler Lib "Kernel32" (ByVal hr As HandlerRoutine, ByVal Add As Boolean) As Boolean

 End Class

#If Testing Then
 Class CtrlTest

      Private Shared MainThread As Thread
      Private Shared CEInterrupt As Boolean
      Public Shared Sub MyHandler(consoleEvent As ConsoleCtrl.ConsoleEvent)

         Console.WriteLine("Event: {0}", consoleEvent)

         If MainThread Is Thread.CurrentThread Then

           Throw New ApplicationException("Event:" & consoleEvent.ToString)
           
         Else
           CEInterrupt = True
           Thread.MemoryBarrier
           MainThread.Abort(CObj(consoleEvent))
           'MainThread.Join
         End If

      End Sub 'MyHandler
      
      
      Public Shared Sub Main()
         Dim cc As New ConsoleCtrl()
         AddHandler cc.ControlEvent, AddressOf MyHandler

         Console.WriteLine("Enter 'E' to exit; 'L' for infinite loop")
         
         While True
            Dim s As String = Console.ReadLine()

            If s = "E" Then
               Exit While
            End If
            If s = "L" Then
               Try
               Try
                 MainThread = Thread.CurrentThread
                 Thread.MemoryBarrier
                 Do
                   'Throw New ApplicationException("Test")
                 Loop

               Catch exa As ThreadAbortException
                 Console.WriteLine("ThreadAbortException in Loop':" & exa.ExceptionState.ToString)
                 If CEInterrupt Then _
                   MainThread.ResetAbort
                 CEInterrupt = False
               Catch ex As Exception
                 Console.WriteLine("Exception in Loop':" & ex.ToString)
               Finally
                 If CEInterrupt Then _
                   MainThread.ResetAbort
                 CEInterrupt = False
               End Try

               Catch exa As ThreadAbortException
                 Console.WriteLine("ThreadAbortException in Loop:" & exa.ExceptionState.ToString)
                 If CEInterrupt Then _
                   MainThread.ResetAbort
                 CEInterrupt = False
               Catch ex As Exception
                 Console.WriteLine("Exception in Loop:" & ex.ToString)
               Finally
                 If CEInterrupt Then _
                   MainThread.ResetAbort
                 CEInterrupt = False
               End Try
            End If
         End While
      End Sub 'Main
 End Class
#End If

    Class REPL

        Shared Private MainThread As Thread
        Shared Private AbortThreadReason As ConsoleCtrl.ConsoleEvent = ConsoleEvent.None

        Public Shared Sub ConsoleHandler(ByVal ConsoleEvent As ConsoleCtrl.ConsoleEventArgs)

         Console.Error.WriteLine("ConsoleEvent: {0}", ConsoleEvent.TheEvent)

         If MainThread Is Nothing OrElse MainThread Is Thread.CurrentThread Then
          Throw New ApplicationException(String.Format("ConsoleEvent: {0}", ConsoleEvent))
         Else
          AbortThreadReason = ConsoleEvent.TheEvent
          Select Case AbortThreadReason
          Case CTRL_C, CTRL_BREAK
            ConsoleEvent.Handled = True
          'Case CTRL_CLOSE, CTRL_LOGOFF, CTRL_SHUTDOWN
          'Case Else
          End Select
          Thread.MemoryBarrier
          MainThread.Abort(CObj(AbortThreadReason))
         End If
        End Sub

        Shared Sub ApEndHandler(sender As Object, args As UnhandledExceptionEventArgs)

         Dim e As Exception = DirectCast(args.ExceptionObject, Exception)

         With e.GetBaseException()
          Console.WriteLine("Exception not (otherwise) handled : " & .GetType().FullName)
          Console.WriteLine(.Message)
         End With

        End Sub

        <STAThread> _
        Shared Sub Main(ByVal args() As String)

            With New Interpreter

                Try
'1.0:                    Dim Path As String
'1.0:                    For Each path In args
'1.1:
                    For Each path As String In args
                        Console.WriteLine(.LoadFile(path).ToString)
                    Next

                Catch e As Exception

                    Console.Error.WriteLine(e.ToString())
                End Try

                Dim cc As New ConsoleCtrl()
                'AddHandler cc.ControlEvent, New ConsoleCtrl.ControlEventHandler(AddressOf ConsoleHandler)
                AddHandler cc.ControlEvent, AddressOf ConsoleHandler
                AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf ApEndHandler
                Do
                    Try
                        Dim r As Object

                        Console.Write("> ")

                        AbortThreadReason = None
                        MainThread = Thread.CurrentThread
                        Thread.MemoryBarrier
                        
                        Try
                            r = .Read("console", Console.In)
                            If .Eof(r) Then Return
                        Catch
                            Console.In.ReadLine
                            Throw
                        End Try

                        Dim x As Object = .Eval(r)
#If UseDotLispStr Then
                        Console.WriteLine(.Str(x))
#Else
                        If Not x Is Nothing Then
                            Console.WriteLine(x.ToString)
                        End If
#End If
                    Catch e As Exception

                        Console.WriteLine("!Exception: " & e.GetBaseException().Message)

                        Select Case AbortThreadReason
                        Case CTRL_C, CTRL_BREAK
                         Thread.ResetAbort
                         AbortThreadReason = None
                        Case CTRL_CLOSE, CTRL_LOGOFF, CTRL_SHUTDOWN
                         Return
                        'Case Else
                         'Ignore
                        End Select

                    End Try
                Loop
            End With
        End Sub
    End Class
End Namespace
