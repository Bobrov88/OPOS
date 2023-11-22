using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPOSControl.Classes;

namespace UPOSControl.USB
{
    public abstract class ScannerTransport
    {
        List<ScannerTransportObserver> Observers = new List<ScannerTransportObserver>();
        bool Ready = false;
        int PingSleepSeconds = 0;


        /// <summary>
        /// Установить частоту пинга
        /// </summary>
        /// <param name="pingSleepSeconds"></param>
        protected void EnablePinging(int pingSleepSeconds)
        {
            this.PingSleepSeconds = pingSleepSeconds;
        }

        /// <summary>
        /// Получить частоту пинга
        /// </summary>
        /// <returns></returns>
        protected int GetPingSleepSeconds()
        {
            return PingSleepSeconds;
        }

        /// <summary>
        /// Устройство было подключено, канал связи удалось установить
        /// </summary>
        protected void OnDeviceConnected()
        {
            Observers.ForEach(x => x.OnDeviceConnected(this));
        }

        /// <summary>
        /// Подключение устройства инициализировано и готово к общению
        /// </summary>
        protected virtual void OnDeviceReady()
        {
            Ready = true;
            Observers.ForEach(x => x.OnDeviceReady(this));
        }

        /// <summary>
        /// Связь с устройством была потеряна: устройство было отключено, выключено или иным образом нарушена линия связи.
        /// </summary>
        protected virtual void OnDeviceDisconnected()
        {
            Ready = false;
            Observers.ForEach(x => x.OnDeviceDisconnected(this));
        }

        /// <summary>
        /// Очистка
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Ошибка на уровне устройства или SDK
        /// </summary>
        /// <param name="code"></param>
        /// <param name="cause"></param>
        /// <param name="message"></param>
        protected void OnDeviceError(int code, Exception cause, string message)
        {
            Observers.ForEach(x => x.OnDeviceError(code, cause, message));
        }

        /// <summary>
        /// Должен вызываться подклассами при получении сообщения.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnMessage(string message)
        {
            Observers.ForEach(x => x.OnMessage(message));
        }

        /// <summary>
        /// Добавьте слушателя к этому передатчику
        /// </summary>
        /// <param name="observer"></param>
        public void Subscribe(ScannerTransportObserver observer)
        {
            if (observer != null && !Observers.Contains(observer))
            {
                ScannerTransport me = this;
                if (Ready)
                {
                    BackgroundWorker bw = new BackgroundWorker();
                    // what to do in the background thread
                    bw.DoWork += delegate
                    {
                        observer.OnDeviceReady(me);
                    };
                    bw.RunWorkerAsync();
                }
                Observers.Add(observer);
            }
        }

        /// <summary>
        /// Удалить слушателя для данного передатчика
        /// </summary>
        /// <param name="observer"></param>
        public void Unsubscribe(ScannerTransportObserver observer)
        {
            if (observer != null && Observers.Contains(observer))
            {
                Observers.Remove(observer);
            }
        }

        public abstract string ShortTitle();

        public virtual string Title => "";
        public virtual string Summary => "";
    }

    public interface ScannerTransportObserver
    {
        /// <summary>
        /// Устройство подключено, но не готово для передачи данных
        /// </summary>
        void OnDeviceConnected(ScannerTransport transport);

        /// <summary>
        /// Устройство готово для передачи данных
        /// </summary>
        void OnDeviceReady(ScannerTransport transport);

        /// <summary>
        /// Устройство отключено
        /// </summary>
        /// <param name="transport"></param>
        void OnDeviceDisconnected(ScannerTransport transport);

        /// <summary>
        /// Пришло сообщение с устройства
        /// </summary>
        /// <param name="message"></param>
        void OnMessage(string message);

        /// <summary>
        /// Вызвана ошибка
        /// </summary>
        /// <param name="code"></param>
        /// <param name="cause"></param>
        /// <param name="message"></param>
        void OnDeviceError(int code, Exception cause, string message);
    }
}
