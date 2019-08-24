﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// <see cref="IAdapterCallContext"/> implementation for ASP.NET Core.
    /// </summary>
    public class AdapterCallContext: IAdapterCallContext {

        /// <summary>
        /// The <see cref="HttpContext"/> associated with the <see cref="AdapterCallContext"/>.
        /// </summary>
        private readonly HttpContext _httpContext;


        /// <inheritdoc/>
        public ClaimsPrincipal User {
            get { return _httpContext.User; }
        }

        /// <inheritdoc/>
        public string ConnectionId {
            get { return _httpContext.Connection.Id; }
        }

        /// <inheritdoc/>
        public IDictionary<object, object> Items {
            get { return _httpContext.Items; }
        }


        /// <summary>
        /// Creates a new <see cref="AdapterCallContext"/> object.
        /// </summary>
        /// <param name="httpContextAccessor">
        ///   The <see cref="IHttpContextAccessor"/> service.
        /// </param>
        public AdapterCallContext(IHttpContextAccessor httpContextAccessor) {
            _httpContext = httpContextAccessor?.HttpContext ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

    }
}
