using ControllerPorts.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.IO.Ports;
using System.Text.Json.Serialization;

namespace ControllerPorts.Controllers
{
    [ApiController]
    [Route("api/comport")]
    public class ComPortController : ControllerBase
    {
        private bool disconnection = false; // переменная для проверки возможности получения данных
        private static SerialPort port; // статическая переменная, которая позволяет работать с портом даже перед инициализацией, чтобы не возникали ошибки
        private readonly IHubContext<ComPortHub> _hubContext; // поле только для чтения класса хаб

        public ComPortController(IHubContext<ComPortHub> hubContext)
        {
            _hubContext = hubContext;
        } // конструктор

        /// <summary>
        ///  Метод отвечает за получения соединения с COM-портом
        /// Связан с методом подключения JS с помощью JSON и Post обработки
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] ComPortSettings settings)
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    port.Close(); // закрывает порт
                    port.Dispose(); // очищает неуправляемые старые данные
                } // чтобы не потерять управление от старого порта, нужно его отключить перед новым подключением, для этого мы отключаем старый порт

                port = new SerialPort(settings.PortName, settings.BaudRate)
                {
                    DataBits = settings.DataBits,
                    Parity = settings.Parity, 
                    StopBits = settings.StopBits,
                    Handshake = settings.Handshake
                };// инициалиащация порта 
                port.DataReceived += (sender, e) => DataReceivedHandler(sender, e, _hubContext); // при получении данных будет срабатывать метод
                port.Open(); // порт открывается

                if (port.IsOpen)
                {
                    return Ok(new { message = "Успешно подключено к " + settings.PortName });
                } // если порт открыт, отсылается информация об этом
                else
                {
                    return BadRequest(new { message = "Ошибка подключения к " + settings.PortName });
                } // иначе отсылается ошибка
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Метод отвечает за отключение соединения с COM-портом
        /// Связан с методом отключения JS с помощью JSON и Post отработки
        /// </summary>
        /// <returns></returns>
        [HttpPost("disconnect")]
        public IActionResult Disconnect()
        {
            try
            {
                disconnection = true; // пока идет отключение, запрашивать данные нельзя

                if (port != null && port.IsOpen)
                {
                    port.Close(); // закрывает порт
                    port.Dispose(); // очищает неуправляемые старые данные
                    if (!port.IsOpen)
                    {
                        return Ok(new { message = "Успешно отключено" }); // при удачном отключении отсылает ответ через метод в textarea(output) в html
                    }
                    else
                    {
                        return BadRequest(new { message = "Ошибка отключения" }); // ошибка отключения
                    }
                } // если порт инициализирован и открыт
                else return BadRequest(new { message = "Нет подключенного порта" }); // если открытого порта нет
            } // попытка отключения 
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            } // при ошибке отключения
            finally
            {
                disconnection = false;
            } // в самом конце, если отключение прошло удачно, снова разрешается запрашивать данные из порта
        }

        /// <summary>
        /// // отвечает за получение строки данных и распределение в нужный textArea
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="hubContext"></param>
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e, IHubContext<ComPortHub> hubContext)
        {
            if (!disconnection)
            {
                SerialPort sp = (SerialPort)sender;
                string indata = sp.ReadLine(); // Запрашивает строку
                var arrData = indata.Split(' '); // Полученная строка разделяется пробелами для проверки
                if (arrData[0] == "ch2_res") hubContext.Clients.All.SendAsync("ReceiveData1", indata); // если это ch2_res параметр, отправляем его асинхронно в pressure(textarea)
                if (arrData[0] == "ch3_res") hubContext.Clients.All.SendAsync("ReceiveData2", indata); // если это ch3_res параметр, отправляем его асинхронно в temperature(textarea)
            } // если отключение не происходит, происходит получение данных и запись

        }
    }

    /// <summary>
    /// Модель, которая содержит в себе поля из Настроек порта
    /// Получает данные из JS с помощью JSON
    /// </summary>
    public class ComPortSettings
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))] // производит конвертацию строкового типа в Enum с помощью JSON 
        public Parity Parity { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))] // производит конвертацию строкового типа в Enum с помощью JSON 
        public StopBits StopBits { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))] // производит конвертацию строкового типа в Enum с помощью JSON 
        public Handshake Handshake { get; set; }
    }
}

