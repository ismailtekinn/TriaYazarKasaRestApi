using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;


namespace TriaYazarKasaRestApi.Business.Libraries.Hugin.Internal
{
    public enum StatusCode
    {
        [Description("ECR satış modunda deği")]
        ST_IDLE = 1,

        [Description("On selling (Ex: cannot get report)")]
        ST_SELLING = 2,

        [Description("Alt toplam alındı")]
        ST_SUBTOTAL = 3,

        [Description("Ödeme bekleniyor")] // odeme yapılmış fakat bitmemiş
        ST_PAYMENT = 4,

        [Description("Fiş kapatılmalı.Ödeme tamalandı.")]
        ST_OPEN_SALE = 5,

        [Description("Fiş bilgisi alınıyor")]
        ST_INFO_RCPT = 6,

        [Description("Özel fiş durumunda")]
        ST_CUSTOM_RCPT = 7,

        [Description("Servis menüsünde")]
        ST_IN_SERVICE = 8,

        [Description("Servis gerekli")]
        ST_SRV_REQUIRED = 9,

        [Description("Kasiyer girişi yapılmadı.")]
        ST_LOGIN = 10,

        [Description("ECR fiş modunda değil")]
        ST_NONFISCAL = 11,

        [Description("İptal edilmesi gereken fiş var.")]
        ST_ON_PWR_RCOVR = 12,

        [Description("Faturada")]
        ST_INVOICE = 13,

        [Description("ECR onaylanmak için bekliyor.")]
        ST_CONFIRM_REQUIRED = 14,
    }
}