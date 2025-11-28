MODULE MainModule
    VAR egmident egmID;
    
    CONST jointtarget jpos1 := [[0,0,0,0,30,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST jointtarget jpos2 := [[10,20,30,0,40,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST jointtarget jpos3 := [[-10,15,25,5,35,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    
    PROC main()
        EGMReset egmID;
        
        WaitTime 2;
        TPWrite "Starting EGM...";
        
        EGMGetId egmID;
        
        EGMSetupUC ROB_1, egmID, "default", "UCdevice";
        
        EGMStreamStart egmID \SampleRate:=4;
        TPWrite "EGM streaming started on port 6510";
        

        WHILE TRUE DO

            MoveAbsJ jpos1, v10, fine, tool0;
            WaitTime 0.5;
            
            MoveAbsJ jpos2, v10, fine, tool0;
            WaitTime 0.5;
            
            MoveAbsJ jpos3, v10, fine, tool0;
            WaitTime 0.5;
            
            MoveAbsJ jpos1, v10, fine, tool0;
            WaitTime 1;
            
            TPWrite "Movement cycle completed";
        ENDWHILE
        
        EGMStreamStop egmID;
        EGMReset egmID;
    ENDPROC
ENDMODULE