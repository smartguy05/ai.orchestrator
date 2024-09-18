using MimeKit;

namespace Ai.Orchestrator.Plugins.Email.Models;

public record MailMessage
{
    /// <summary>
    /// Get or set the value of the Priority header.
    /// </summary>
    /// <remarks>
    /// Gets or sets the value of the Priority header.
    /// </remarks>
    /// <value>The priority.</value>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <paramref name="value"/> is not a valid <see cref="MessagePriority"/>.
    /// </exception>
    public MessagePriority Priority { get; init; }
    /// <summary>
    /// Get or set the address in the Sender header.
    /// </summary>
    /// <remarks>
    /// The sender may differ from the addresses in <see cref="From"/> if
    /// the message was sent by someone on behalf of someone else.
    /// </remarks>
    /// <value>The address in the Sender header.</value>
    public string Sender { get; init; }
    /// <summary>
    /// Get the list of addresses in the From header.
    /// </summary>
    /// <remarks>
    /// <para>The "From" header specifies the author(s) of the message.</para>
    /// <para>If more than one <see cref="MailboxAddress"/> is added to the
    /// list of "From" addresses, the <see cref="Sender"/> should be set to the
    /// single <see cref="MailboxAddress"/> of the personal actually sending
    /// the message.</para>
    /// </remarks>
    /// <value>The list of addresses in the From header.</value>
    public string From { get; init; }
    /// <summary>
    /// Get the list of addresses in the Reply-To header.
    /// </summary>
    /// <remarks>
    /// <para>When the list of addresses in the Reply-To header is not empty,
    /// it contains the address(es) where the author(s) of the message prefer
    /// that replies be sent.</para>
    /// <para>When the list of addresses in the Reply-To header is empty,
    /// replies should be sent to the mailbox(es) specified in the From
    /// header.</para>
    /// </remarks>
    /// <value>The list of addresses in the Reply-To header.</value>
    public string ReplyTo { get; init; }
    /// <summary>
    /// Get the list of addresses in the To header.
    /// </summary>
    /// <remarks>
    /// The addresses in the To header are the primary recipients of
    /// the message.
    /// </remarks>
    /// <value>The list of addresses in the To header.</value>
    public string To { get; init; }
    /// <summary>
    /// Get the list of addresses in the Bcc header.
    /// </summary>
    /// <remarks>
    /// Recipients in the Blind-Carbon-Copy list will not be visible to
    /// the other recipients of the message.
    /// </remarks>
    /// <value>The list of addresses in the Bcc header.</value>
    public string Bcc { get; init; }
    /// <summary>
    /// Get or set the subject of the message.
    /// </summary>
    /// <remarks>
    /// <para>The Subject is typically a short string denoting the topic of the message.</para>
    /// <para>Replies will often use <c>"Re: "</c> followed by the Subject of the original message.</para>
    /// </remarks>
    /// <value>The subject of the message.</value>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public string Subject { get; init; }
    /// <summary>
    /// Get or set the date of the message.
    /// </summary>
    /// <remarks>
    /// If the date is not explicitly set before the message is written to a stream,
    /// the date will default to the exact moment when it is written to said stream.
    /// </remarks>
    /// <value>The date of the message.</value>
    public DateTimeOffset Date { get; init; }
    /// <summary>
    /// Get or set the message identifier.
    /// </summary>
    /// <remarks>
    /// <para>The Message-Id is meant to be a globally unique identifier for
    /// a message.</para>
    /// <para><see cref="MimeUtils.GenerateMessageId()"/> can be used
    /// to generate this value.</para>
    /// </remarks>
    /// <value>The message identifier.</value>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// <paramref name="value"/> is improperly formatted.
    /// </exception>
    public string MessageId { get; init; }
    /// <summary>
    /// Get or set the body of the message.
    /// </summary>
    /// <remarks>
    /// <para>The body of the message can either be plain text or it can be a
    /// tree of MIME entities such as a text/plain MIME part and a collection
    /// of file attachments.</para>
    /// <para>For a convenient way of constructing message bodies, see the
    /// <see cref="BodyBuilder"/> class.</para>
    /// </remarks>
    /// <value>The body of the message.</value>
    public string Body { get; init; }
    /// <summary>
    /// Get the attachments.
    /// </summary>
    /// <remarks>
    /// Traverses over the MIME tree, enumerating all of the <see cref="MimeEntity"/> objects that
    /// have a Content-Disposition header set to <c>"attachment"</c>.
    /// </remarks>
    /// <example>
    /// <code language="c#" source="Examples\AttachmentExamples.cs" region="SaveAttachments" />
    /// </example>
    /// <value>The attachments.</value>
    public bool HasAttachments { get; init; }
    public IEnumerable<string> Attachments { get; init; }
}