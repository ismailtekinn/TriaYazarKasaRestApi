using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace TriaYazarKasaRestApi.Business.Libraries.Hugin.Internal
{
    public class CPResponse
    {
        private const char SplitChar = '|';

        private int _paramIndex;
        private readonly int _errorCode;
        private readonly int _statusCode;
        private readonly List<string?> _paramList = new();

        public int ErrorCode => _errorCode;
        public int StatusCode => _statusCode;
        public List<string?> ParamList => _paramList;

        public ErrorCode EnumErrorCode => (ErrorCode)_errorCode;
        public StatusCode EnumStatusCode => (StatusCode)_statusCode;

        public string ErrorMessage
        {
            get
            {
                try
                {
                    return DescriptionAttr((ErrorCode)_errorCode);
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public string StatusMessage
        {
            get
            {
                try
                {
                    return DescriptionAttr((StatusCode)_statusCode);
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public int CurrentParamIndex => _paramIndex;

        public CPResponse(string? response)
        {
            _paramIndex = 0;

            if (string.IsNullOrWhiteSpace(response))
                return;

            var parts = response.Split(SplitChar);

            for (var i = 0; i < parts.Length; i++)
            {
                var item = parts[i];

                if (i == 0)
                {
                    if (int.TryParse(item, out var error))
                        _errorCode = error;
                }
                else if (i == 1)
                {
                    if (int.TryParse(item, out var status))
                        _statusCode = status;
                }
                else
                {
                    _paramList.Add(string.IsNullOrEmpty(item) ? null : item);
                }
            }
        }

        public string? GetNextParam()
        {
            if (_paramIndex >= _paramList.Count)
                return null;

            var value = _paramList[_paramIndex];
            _paramIndex++;
            return value;
        }

        public string? GetParamByIndex(int index)
        {
            if (index <= 0 || index > _paramList.Count)
                return null;

            return _paramList[index - 1];
        }

        private static string DescriptionAttr<T>(T source)
        {
            var fieldInfo = source?.GetType().GetField(source.ToString()!);
            if (fieldInfo == null)
                return string.Empty;

            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : string.Empty;
        }
    }
}
