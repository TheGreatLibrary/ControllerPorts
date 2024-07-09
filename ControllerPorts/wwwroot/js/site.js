const output = document.getElementById('output');
const pressure = document.getElementById('pressure');
const temperature = document.getElementById('temperature');

// Инициализируется объект класса ComPortHub для установки соединения с хабом для получения данных
const connection = new signalR.HubConnectionBuilder().withUrl("/comPortHub").build();

// метод передачи данных в поле Давление
connection.on("ReceiveData1", (data) => {
    pressure.value += data + '\n';
    autoScroll(pressure);
});

// метод передачи данных в поле Давление
connection.on("ReceiveData2", (data) => {
    temperature.value += data + '\n';
    autoScroll(temperature);
});

// метод для начала получения данных
connection.start().catch(err => console.error(err.toString()));

function connectToComPort() {
    let portName = document.getElementById('portName').value;
    let baudRate = document.getElementById('baudRate').value;
    let dataBits = document.getElementById('dataBits').value;
    let parity = document.getElementById('parity').value;
    let stopBits = document.getElementById('stopBits').value;
    let handshake = document.getElementById('handshake').value;

    // идет установка с методом Connect в C#, отправляются данные из select-html
    fetch('/api/comport/connect', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ portName, baudRate, parity, dataBits, stopBits, handshake})
    })
    .then(response => response.json())
    .then(data => {
        output.value += data.message + "\n";
    }) // при удачном подключении в output вносится сообщение об этом
    .catch(error => {
        output.value += 'Error: ' + error + "\n";
    }); // при ошибке также выводится сообщение
    autoScroll(output);
} // метод подключения порта

function disconnectFromComPort() {
    // идет установка через JSON с методом Disconnect в C#
    fetch('/api/comport/disconnect', {
        method: 'POST'
    })
        .then(response => response.json())
        .then(data => {
            output.value += data.message + '\n';
        }) // при удачном подключении в output вносится сообщение об этом
        .catch(error => {
            output.value += 'Error: ' + error + '\n';
        }); // при ошибке также выводится сообщение
    autoScroll(output);
} // метод отключения порта

function clearOutput() {
    pressure.value = "";
    temperature.value = "";
    output.value = "";
} // метод очистки полей

function autoScroll(textarea) {
    textarea.scrollTop = textarea.scrollHeight;
} // метод автоматического скроллинга при добавлении нового значения в textarea