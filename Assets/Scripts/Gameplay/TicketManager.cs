using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TicketManager 
{
    public static Action<int> onTicketChange;
    private static int _ticket;
    public static int totalTicket
    {
        get
        {
            return _ticket;
        }
        set
        {
            _ticket = value;
            onTicketChange?.Invoke(value);
            DataManager.data.ticketCount = value;
        }
    }
}
