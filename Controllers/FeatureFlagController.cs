using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using openspace.Hubs;
using openspace.Models;
using openspace.Repositories;

namespace openspace.Controllers
{
    [Route("api/featureflags")]
    public class FeatureFlagController:Controller
    {
        private readonly IConfiguration _configuration;

        public FeatureFlagController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IEnumerable<FeatureFlag> Get() => _configuration.GetSection("FeatureFlags").Get<IEnumerable<FeatureFlag>>();
    }
}