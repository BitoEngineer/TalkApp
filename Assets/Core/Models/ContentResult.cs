using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Core.Server
{
    /// <summary>
    /// Content result enum used in <see cref="JsonPacket"/>.
    /// </summary>
    public enum ContentResult : byte
    {
        /// <summary>
        /// <see cref="None"/> is used by the server when no reply should be sent.
        /// Or when the packet sent is not a reply.
        /// </summary>
        None = 0,

        /// <summary>
        /// Returned when the Packet Client ID doesn't exists in Users database.
        /// <para>URI "CreateUser" ignores this validation, so use it to create users.</para>
        /// </summary>
        NotAuthorized,

        /// <summary>
        /// Returned when everything is OK.
        /// </summary>
        OK,

        /// <summary>
        /// Returned when the received URI does not exist.
        /// </summary>
        BadRequest,

        /// <summary>
        /// Returned when the call content is invalid.
        /// </summary>
        BadContent,

        /// <summary>
        /// Returned when content is expected and there's none.
        /// </summary>
        NoContent,

        /// <summary>
        /// <see cref="Forbidden"/> may be used to indicate that the given user is not able to perform such call.
        /// </summary>
        Forbidden,

        /// <summary>
        /// <see cref="Created"/> may be used to indicate that a given call created something successfully.
        /// </summary>
        Created,

        /// <summary>
        /// <see cref="NotFound"/> may be used to indicate that a given call haven't found something.
        /// </summary>
        NotFound,

        /// <summary>
        /// <see cref="Conflict"/> may be used to indicate that the given call data conflicts with something. 
        /// </summary>
        Conflict,

        /// <summary>
        /// Returned whenever an unexpected error occurs in the call execution, for instance an unhandled expection.
        /// </summary>
        InternalServerError
    };
}
