using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Bookkeeping.Infrastructure.Mono
{
    public class MonoOptions
    {
        public const string SectionName = "Transactions:Mono";

        [Required, Url]
        public string BaseUrl { get; init; } = "https://api.withmono.com/";

        [Required]
        public string SecretKey { get; init; } = default!;

        [Required]
        public string WebhookSecret { get; init; } = default!;
    }
}