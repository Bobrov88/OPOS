using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Management;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Timers;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using LibUsbDotNet.WinUsb;
using UPOSControl.Enums;
using UPOSControl.Managers;
using UPOSControl.Classes;
using MonoLibUsb;
using MonoLibUsb.Profile;

namespace UPOSControl.USB
{
    
    class BlockingQueue<T>
    {
        private readonly Queue<T> queue = new Queue<T>();

        public void Enqueue(T item)
        {
            lock (this)
            {
                queue.Enqueue(item);
                Monitor.PulseAll(this);
            }
        }

        public T Peek()
        {
            lock (this)
            {
                return queue.Peek();
            }
        }

        /// <summary>
        /// Может возвращать значение null, если поток уведомлен / отправлен импульсом или истекает время ожидания
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            lock (this)
            {
                return queue.Dequeue();
            }
        }

        public bool DequeueIf(T value)
        {
            lock (this)
            {
                var peek = queue.Peek();
                if (object.Equals(peek, value))
                {
                    queue.Dequeue();
                    return true;
                }
                return false;
            }
        }

        public int Count
        {
            get
            {
                lock (this)
                {
                    return queue.Count;
                }
            }
        }
    }

}
