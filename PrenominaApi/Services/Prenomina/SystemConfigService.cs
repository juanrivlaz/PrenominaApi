using Newtonsoft.Json;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.Reports;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class SystemConfigService : ServicePrenomina<SystemConfig>
    {
        //private static IZKEM? zkemkeeper;
        public SystemConfigService(IBaseRepositoryPrenomina<SystemConfig> baseRepository) : base(baseRepository)
        {
            /*zkemkeeper = (IZKEM?)Activator.CreateInstance(Type.GetTypeFromProgID("zkemkeeper.ZKEM"), true);

            if (zkemkeeper != null) {
                bool connected = false; // zkemkeeper.Connect_Net("192.168.100.99", 4370);

                if (connected)
                {
                    var resultCreateSMS = zkemkeeper.SetSMS(1, 1, 253, 10, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Hola Prueba");
                    var resultSendSMS = zkemkeeper.SetUserSMS(1, 1, 1);
                    var resultSendSMS2 = zkemkeeper.SSR_SetUserSMS(1, "1", 1);

                    string employeId = string.Empty;
                    string name = string.Empty;
                    string password = string.Empty;
                    int permission = 0;
                    bool active = false;
                    string empty4 = string.Empty;
                    int longitudHuella = 0;
                    int fingerFlag = 0;
                    string fingerBase64 = string.Empty;

                    // Get user info
                    zkemkeeper.SSR_GetAllUserInfo(1, out employeId, out name, out password, out permission, out active);

                    Console.WriteLine(new { employeId, name, password, permission, active });

                    // Get user fingers
                    for (int fingerIndex = 0; fingerIndex < 10; fingerIndex++)
                    {
                        if (zkemkeeper.GetUserTmpExStr(1, employeId, fingerIndex, out fingerFlag, out fingerBase64, out longitudHuella))
                        {
                            Console.WriteLine(new { fingerBase64, longitudHuella });
                        }
                    }

                    // Get users logs (checks)
                    int year = 0;
                    int month = 0;
                    int day = 0;
                    int hour = 0;
                    int minute = 0;
                    int second = 0;
                    int verifyMode = 0;
                    int inOutMode = 0;
                    int workCode = 0;

                    while(zkemkeeper.SSR_GetGeneralLogData(1, out employeId, out verifyMode, out inOutMode, out year, out month, out day, out hour, out minute, out second, ref workCode))
                    {
                        Console.WriteLine(new
                        {
                            year,
                            month,
                            day,
                            hour,
                            minute,
                            second,
                            verifyMode,//15 face, 1 finger
                            inOutMode,//0 in, 1 out
                            workCode
                        });
                    }

                    zkemkeeper.Disconnect();
                }
            }*/
        }

        public SysYearOperation ExecuteProcess(string key)
        {
            var year = DateTime.Now.Year;
            var findObject = _repository.GetById(key);
            if (findObject != null) {
                var value = JsonConvert.DeserializeObject<SysYearOperation>(findObject.Data);
                year = value?.Year ?? year;
            }

            return new SysYearOperation
            {
                TypeData = "Int",
                Year = year,
            };
        }

        public SysConfigReports ExecuteProcess(GetConfigReport config)
        {
            SysConfigReports configResult = new SysConfigReports();

            var findObject = _repository.GetById(SysConfig.ConfigReports);
            if (findObject != null)
            {
                var result = JsonConvert.DeserializeObject<SysConfigReports>(findObject.Data);

                if (result != null) {
                    configResult = result;
                }
            }

            return configResult;
        }

        public bool ExecuteProcess(ClockInterval clockInterval)
        {
            var setting = _repository.GetByFilter(sc => sc.Key == SysConfig.ExtractChecks).FirstOrDefault();
            if (setting != null)
            {
                setting.Data = JsonConvert.SerializeObject(new SysExtractCheck()
                {
                    IntervalInMinutes = clockInterval.Minutes,
                });

                _repository.Update(setting);
                _repository.Save();
            }

            return true;
        }

        public bool ExecuteProcess(UpdateLogo updateLogo)
        {
            var setting = _repository.GetByFilter(sc => sc.Key == SysConfig.Logo).FirstOrDefault();
            if (setting != null)
            {
                setting.Data = JsonConvert.SerializeObject(new SysLogo()
                {
                    Logo = ""
                });

                _repository.Update(setting);
                _repository.Save();
            }

            return true;
        }

        public bool ExecuteProcess(UpdateTypeTenant updateTypeTenant)
        {
            var setting = _repository.GetByFilter(sc => sc.Key == SysConfig.TypeTenant).FirstOrDefault();
            if (setting != null)
            {
                setting.Data = JsonConvert.SerializeObject(new SysTypeTenant()
                {
                    TypeTenant = updateTypeTenant.TypeTenant,
                });

                _repository.Update(setting);
                _repository.Save();
            }

            return true;
        }

        public bool ExecuteProcess(EditTypeDayOffReport editTypeDayOffReport) {
            var findObject = _repository.GetById(SysConfig.ConfigReports);

            SysConfigReports configResult = new SysConfigReports()
            {
                ConfigDayOffReport = new ConfigDayOffReport()
                {
                    TypeDayOffReport = editTypeDayOffReport.TypeDayOffReport,
                }
            };

            if (findObject != null)
            {
                var parser = JsonConvert.DeserializeObject<SysConfigReports>(findObject.Data);

                if (parser != null)
                {
                    configResult = parser;
                    configResult.ConfigDayOffReport.TypeDayOffReport = editTypeDayOffReport.TypeDayOffReport;
                }


                findObject.Data = JsonConvert.SerializeObject(configResult);

                _repository.Update(findObject);
                _repository.Save();
            }

            return true;
        }

        public bool ExecuteProcess(EditMinsToOvertimeReport editMinsToOvertimeReport)
        {
            var findObject = _repository.GetById(SysConfig.ConfigReports);

            if (findObject != null)
            {
                var parser = JsonConvert.DeserializeObject<SysConfigReports>(findObject.Data);

                if (parser != null)
                {
                    parser.ConfigOvertimeReport.Mins = editMinsToOvertimeReport.Minutes;

                    findObject.Data = JsonConvert.SerializeObject(parser);
                    _repository.Update(findObject);
                    _repository.Save();
                }
            }

            return true;
        }
    }
}
