using Hugin.Common;
using Hugin.POS.CompactPrinter.FP300;
using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using TriaYazarKasaRestApi.Business;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Business.Models;
using TriaYazarKasaRestApi.Business.Libraries.Hugin.Internal;
using System.Text.Json;
using System.Text.Json.Serialization;
using TriaYazarKasaRestApi.Entities.DTOs;
namespace TriaYazarKasaRestApi.Business.Adapters
{
    public class HuginAdapter : IHuginAdapter
    {
        private IConnection? _connection;
        private ICompactPrinter? _printer;
        private AdapterConnectionInfo? _connectionInfo;
        private bool _isConnected;
        private bool _isMatchedBefore;
        private string? _lastError;
        private const int TimeoutSeconds = 30;

        public bool IsConnected => _isConnected;

        public Task<PosOperationResult> ConnectAsync(AdapterConnectionInfo connectionInfo)
        {
            try
            {
                _connectionInfo = connectionInfo;

                if (string.IsNullOrWhiteSpace(connectionInfo.FiscalId))
                    return Task.FromResult(PosOperationResult.Fail("FiscalId bos olamaz."));

                if (connectionInfo.UseTcp)
                {
                    if (string.IsNullOrWhiteSpace(connectionInfo.IpAddress) || connectionInfo.Port <= 0)
                        return Task.FromResult(PosOperationResult.Fail("TCP baglantisi icin IpAddress ve Port zorunludur."));

                    _connection = new TCPConnection(connectionInfo.IpAddress, connectionInfo.Port);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(connectionInfo.SerialPort))
                        return Task.FromResult(PosOperationResult.Fail("Seri baglanti icin SerialPort zorunludur."));

                    _connection = new SerialConnection(connectionInfo.SerialPort, 115200);
                }

                _connection.Open();

                MatchExDevice(connectionInfo.FiscalId);

                _isConnected = true;
                _lastError = null;

                return Task.FromResult(PosOperationResult.Ok("Hugin baglantisi basarili.", new
                {
                    connectionType = connectionInfo.UseTcp ? "TCP" : "Serial",
                    ip = connectionInfo.IpAddress,
                    port = connectionInfo.Port,
                    serialPort = connectionInfo.SerialPort,
                    fiscalId = connectionInfo.FiscalId
                }));
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _lastError = BuildDetailedErrorMessage(ex);
                return Task.FromResult(PosOperationResult.Fail($"Hugin baglanti hatasi: {_lastError}"));
            }
        }

        public Task<PosOperationResult> DisconnectAsync()
        {
            try
            {
                if (_connection != null && _connection.IsOpen)
                    _connection.Close();

                _printer = null;
                _connection = null;
                _isConnected = false;
                _lastError = null;

                return Task.FromResult(PosOperationResult.Ok("Hugin baglantisi kapatildi."));
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return Task.FromResult(PosOperationResult.Fail($"Baglanti kapatilirken hata olustu: {ex.Message}"));
            }
        }

        public Task<PosOperationResult> GetStatusAsync()
        {
            try
            {
                if (!EnsureConnected(out var error))
                    return Task.FromResult(PosOperationResult.Fail(error));

                var response = new CPResponse(_printer!.CheckPrinterStatus());
                var statusCode = response.EnumStatusCode;
                var errorCode = response.EnumErrorCode;

                return Task.FromResult(PosOperationResult.Ok("Durum bilgisi alindi.", new
                {
                    isConnected = _isConnected,
                    connectionOpen = _connection?.IsOpen ?? false,
                    fiscalId = _connectionInfo?.FiscalId,
                    connectionType = _connectionInfo?.UseTcp == true ? "TCP" : "Serial",
                    rawStatusCode = response.StatusCode,
                    statusCode = statusCode.ToString(),
                    statusMessage = response.StatusMessage,
                    deviceState = MapDeviceState(statusCode),
                    hasActiveOperation = HasActiveOperation(statusCode),
                    rawErrorCode = response.ErrorCode,
                    error = errorCode.ToString(),
                    errorMessage = response.ErrorMessage,
                    isReadyForNewSale = statusCode == StatusCode.ST_IDLE && errorCode == ErrorCode.ERR_SUCCESS
                }));
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return Task.FromResult(PosOperationResult.Fail($"Durum bilgisi alinamadi: {ex.Message}"));
            }
        }

        public Task<PosOperationResult> GetDeviceInfoAsync()
        {
            try
            {
                if (!EnsureConnected(out var error))
                    return Task.FromResult(PosOperationResult.Fail(error));

                var version = _printer!.GetECRVersion();

                return Task.FromResult(PosOperationResult.Ok("Cihaz bilgisi alindi.", new
                {
                    brand = "Hugin",
                    fiscalId = _connectionInfo?.FiscalId,
                    version = version,
                    connectionType = _connectionInfo?.UseTcp == true ? "TCP" : "Serial"
                }));
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return Task.FromResult(PosOperationResult.Fail($"Cihaz bilgisi alinamadi: {ex.Message}"));
            }
        }

        public Task<PosOperationResult> GetXReportAsync()
        {
            try
            {
                if (!EnsureConnected(out var error))
                    return Task.FromResult(PosOperationResult.Fail(error));

                var statusResponse = new CPResponse(_printer!.CheckPrinterStatus());
                var reportResponse = new CPResponse(_printer.PrintXReport(3));

                return Task.FromResult(PosOperationResult.Ok("Hugin X raporu alindi.", new
                {
                    statusCode = statusResponse.EnumStatusCode.ToString(),
                    errorCode = reportResponse.ErrorCode
                }));
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return Task.FromResult(PosOperationResult.Fail($"X raporu alinamadi: {ex.Message}"));
            }
        }

        public Task<PosOperationResult> GetZReportAsync()
        {
            try
            {
                if (!EnsureConnected(out var error))
                    return Task.FromResult(PosOperationResult.Fail(error));

                var statusResponse = new CPResponse(_printer!.CheckPrinterStatus());
                var reportResponse = new CPResponse(_printer.PrintZReport(3));

                return Task.FromResult(PosOperationResult.Ok("Hugin Z raporu alindi.", new
                {
                    statusCode = statusResponse.EnumStatusCode.ToString(),
                    errorCode = reportResponse.ErrorCode
                }));
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return Task.FromResult(PosOperationResult.Fail($"Z raporu alinamadi: {ex.Message}"));
            }
        }

        public Task<PosOperationResult> SendJsonDocumentAsync(HuginJsonDocumentRequestDto request)
        {
            try
            {
                if (!EnsureConnected(out var error))
                    return Task.FromResult(PosOperationResult.Fail(error));

                if (request == null)
                    return Task.FromResult(PosOperationResult.Fail("Json document request bos olamaz."));

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonStr = JsonSerializer.Serialize(request, jsonOptions);

                var response = new CPResponse(_printer!.PrintJSONDocumentDeptOnly(jsonStr));

                return Task.FromResult(PosOperationResult.Ok("Json belge cihaza gonderildi.", new
                {
                    requestJson = jsonStr,
                    errorCode = response.ErrorCode,
                    statusCode = response.EnumStatusCode.ToString(),
                    error = response.EnumErrorCode.ToString(),
                    errorMessage = response.ErrorMessage
                }, response.ErrorCode));
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return Task.FromResult(PosOperationResult.Fail($"Json belge gonderilemedi: {ex.Message}"));
            }
        }

        // ödeme ile ilgili methotlar 
        public Task<PosOperationResult> AddCashPaymentAsync(HuginCashPaymentRequestDto request)
        {
            try
            {
                if (!EnsureConnected(out var error))
                    return Task.FromResult(PosOperationResult.Fail(error));

                if (request == null || request.Amount <= 0)
                    return Task.FromResult(PosOperationResult.Fail("Gecerli bir nakit odeme tutari gonderilmelidir."));

                var response = new CPResponse(_printer!.PrintPayment(1, 0, request.Amount, request.Description ?? string.Empty));

                if (response.EnumErrorCode != ErrorCode.ERR_SUCCESS)
                {
                    return Task.FromResult(PosOperationResult.Fail(
                        $"Nakit odeme eklenemedi: {response.ErrorMessage}",
                        response.ErrorCode));
                }

                return Task.FromResult(PosOperationResult.Ok("Nakit odeme eklendi.", new
                {
                    amount = request.Amount,
                    description = request.Description,
                    errorCode = response.ErrorCode,
                    statusCode = response.EnumStatusCode.ToString(),
                    error = response.EnumErrorCode.ToString(),
                    errorMessage = response.ErrorMessage
                }, response.ErrorCode));
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return Task.FromResult(PosOperationResult.Fail($"Nakit odeme eklenemedi: {ex.Message}"));
            }
        }

        public Task<PosOperationResult> AddCardPaymentAsync(HuginCardPaymentRequestDto request)
        {
            try
            {
                if (!EnsureConnected(out var error))
                    return Task.FromResult(PosOperationResult.Fail(error));

                if (request == null || request.Amount <= 0)
                    return Task.FromResult(PosOperationResult.Fail("Gecerli bir kredi karti odeme tutari gonderilmelidir."));

                var response = new CPResponse(
                    _printer!.GetEFTAuthorisation(
                        request.Amount,
                        request.Installment,
                        request.CardNumber ?? string.Empty));

                if (response.EnumErrorCode != ErrorCode.ERR_SUCCESS)
                {
                    return Task.FromResult(PosOperationResult.Fail(
                        $"Kredi karti odemesi alinamadi: {response.ErrorMessage}",
                        response.ErrorCode));
                }

                var totalAmount = response.GetNextParam();
                var provisionCode = response.GetNextParam();
                var paidAmount = response.GetNextParam();
                var installmentCount = response.GetNextParam();
                var acquirerId = response.GetNextParam();
                var bin = response.GetNextParam();
                var issuerId = response.GetNextParam();
                var subOprtType = response.GetNextParam();
                var batch = response.GetNextParam();
                var stan = response.GetNextParam();
                var totalPaidAmount = response.GetNextParam();

                return Task.FromResult(PosOperationResult.Ok("Kredi karti odemesi alindi.", new
                {
                    totalAmount,
                    provisionCode,
                    paidAmount,
                    installmentCount,
                    acquirerId,
                    bin,
                    issuerId,
                    subOprtType,
                    batch,
                    stan,
                    totalPaidAmount,
                    errorCode = response.ErrorCode,
                    statusCode = response.EnumStatusCode.ToString(),
                    error = response.EnumErrorCode.ToString(),
                    errorMessage = response.ErrorMessage
                }, response.ErrorCode));
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return Task.FromResult(PosOperationResult.Fail($"Kredi karti odemesi alinamadi: {ex.Message}"));
            }
        }

        public Task<PosOperationResult> CloseReceiptAsync()
        {
            try
            {
                if (!EnsureConnected(out var error))
                    return Task.FromResult(PosOperationResult.Fail(error));

                var response = new CPResponse(_printer!.CloseReceipt(false));

                if (response.EnumErrorCode != ErrorCode.ERR_SUCCESS)
                {
                    return Task.FromResult(PosOperationResult.Fail(
                        $"Fis kapatilamadi: {response.ErrorMessage}",
                        response.ErrorCode));
                }

                return Task.FromResult(PosOperationResult.Ok("Fis kapatildi.", new
                {
                    errorCode = response.ErrorCode,
                    statusCode = response.EnumStatusCode.ToString(),
                    error = response.EnumErrorCode.ToString(),
                    errorMessage = response.ErrorMessage
                }, response.ErrorCode));
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return Task.FromResult(PosOperationResult.Fail($"Fis kapatilamadi: {ex.Message}"));
            }
        }

        private bool EnsureConnected(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_isConnected || _printer == null || _connection == null || !_connection.IsOpen)
            {
                errorMessage = "Hugin cihazi bagli degil.";
                return false;
            }

            return true;
        }

        private void MatchExDevice(string fiscalId)
        {
            if (_connection == null || !_connection.IsOpen)
                throw new InvalidOperationException("Connection acik degil.");

            var serverInfo = new DeviceInfo
            {
                Brand = "HUGIN",
                Model = "HUGIN COMPACT",
                Version = "REST API",
                IPProtocol = IPProtocol.IPV4,
                IP = IPAddress.Parse(GetLocalIpAddress()),
                Port = _connectionInfo.UseTcp ? _connectionInfo.Port : 0,
                TerminalNo = fiscalId.PadLeft(8, '0'),
                SerialNum = CreateDeviceSerial()
            };

            if (_isMatchedBefore && _printer != null)
            {
                _printer.SetCommObject(_connection.ToObject());
                _printer.SendTimeOutInformation(TimeoutSeconds.ToString());
                return;
            }

            _printer = new CompactPrinter
            {
                FiscalRegisterNo = fiscalId
            };

            if (!_printer.Connect(_connection.ToObject(), serverInfo))
                throw new Exception("Hugin eslestirme basarisiz.");

            if (_printer.PrinterBufferSize != _connection.BufferSize)
                _connection.BufferSize = _printer.PrinterBufferSize;

            _printer.SetCommObject(_connection.ToObject());
            _printer.SendTimeOutInformation(TimeoutSeconds.ToString());

            _isMatchedBefore = true;
        }

        private string CreateDeviceSerial()
        {
            try
            {
                return CreateMd5(GetMachineIdentity()).Substring(0, 8);
            }
            catch
            {
                return "ABCD1234";
            }
        }

        private static string GetMachineIdentity()
        {
            const string machineGuidPath = @"SOFTWARE\Microsoft\Cryptography";
            const string machineGuidName = "MachineGuid";
            using var localMachineKey = Registry.LocalMachine.OpenSubKey(machineGuidPath);
            var machineGuid = localMachineKey?.GetValue(machineGuidName)?.ToString();
            if (!string.IsNullOrWhiteSpace(machineGuid))
            {
                return machineGuid;
            }

            return Environment.MachineName;
        }

        private static string GetLocalIpAddress()
        {
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ipAddress in hostEntry.AddressList)
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    return ipAddress.ToString();
            }

            return "127.0.0.1";
        }

        private static string CreateMd5(string input)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.ASCII.GetBytes(input));
            var builder = new StringBuilder();

            foreach (var item in bytes)
                builder.Append(item.ToString("X2"));

            return builder.ToString();
        }

        private static string BuildDetailedErrorMessage(Exception exception)
        {
            var messages = new List<string>();
            Exception? current = exception;

            while (current != null)
            {
                if (current is SocketException socketException)
                {
                    messages.Add($"{current.Message} (SocketError: {socketException.SocketErrorCode}, NativeError: {socketException.ErrorCode})");
                }
                else
                {
                    messages.Add(current.Message);
                }

                current = current.InnerException;
            }

            return string.Join(" --> ", messages.Distinct());
        }

        private static bool HasActiveOperation(StatusCode statusCode)
        {
            return statusCode is StatusCode.ST_SELLING
                or StatusCode.ST_SUBTOTAL
                or StatusCode.ST_PAYMENT
                or StatusCode.ST_OPEN_SALE
                or StatusCode.ST_INFO_RCPT
                or StatusCode.ST_CUSTOM_RCPT
                or StatusCode.ST_INVOICE
                or StatusCode.ST_CONFIRM_REQUIRED;
        }

        private static string MapDeviceState(StatusCode statusCode)
        {
            return statusCode switch
            {
                StatusCode.ST_IDLE => "Hazir, yeni satis baslatilabilir.",
                StatusCode.ST_SELLING => "Satis ekrani acik, fis uzerinde islem devam ediyor.",
                StatusCode.ST_SUBTOTAL => "Ara toplam alinmis, odeme asamasina gecilebilir.",
                StatusCode.ST_PAYMENT => "Odeme ekrani acik, odeme bekleniyor.",
                StatusCode.ST_OPEN_SALE => "Odeme tamamlanmis, fis kapatilmali.",
                StatusCode.ST_INFO_RCPT => "Fis detaylari okunuyor.",
                StatusCode.ST_CUSTOM_RCPT => "Ozel fis modunda islem var.",
                StatusCode.ST_IN_SERVICE => "Cihaz servis modunda.",
                StatusCode.ST_SRV_REQUIRED => "Servis mudahalesi gerekiyor.",
                StatusCode.ST_LOGIN => "Kasiyer girisi bekleniyor.",
                StatusCode.ST_NONFISCAL => "Cihaz mali fis modunda degil.",
                StatusCode.ST_ON_PWR_RCOVR => "Yarim kalan fis iptal veya toparlama bekliyor.",
                StatusCode.ST_INVOICE => "Fatura islemi devam ediyor.",
                StatusCode.ST_CONFIRM_REQUIRED => "Cihaz kullanicidan onay bekliyor.",
                _ => "Bilinmeyen cihaz durumu."
            };
        }
    }
}
