// Learn more about F# at http://fsharp.org
namespace emailtest
open System

module EmailType =

    open System

    type SenderEmailAddress = SenderEmailAddress of string
    type RecipientName = RecipientName of string
    type RecipientEmailAddress = RecipientEmailAddress of string

    type Sender =
        { email : SenderEmailAddress
          password : Security.SecureString }

    type Recipient =
        { name : RecipientName
          email : RecipientEmailAddress }

    type Subject = Subject of string
    type Body = Body of string

    type Email =
        { sender : Sender
          recipients : Recipient[]
          subject : Subject
          body : Body }

module Email =
    open System.Reflection
    open System.Collections.Generic
    open System
    open System.Net
    open System.Net.Mail
    open System.IO
    open EmailType
    
    let secureString (str : string) = NetworkCredential("", str).SecurePassword
    
    let sendEmail (SenderEmailAddress senderEmail) (password : Security.SecureString) recipients (Subject subject) (Body body) =
        async {
            let sender = MailAddress(senderEmail)

            use message = new MailMessage()
            message.From <- sender
            recipients
            |> Array.map (fun r -> 
                let (RecipientEmailAddress addr) = r.email
                MailAddress addr)
            |> Array.iter message.To.Add
            message.Subject <- subject
            message.Body <- body
            message.IsBodyHtml <- true
    
            use smtp = new SmtpClient()
            smtp.Host <- "smtp.gmail.com"
            smtp.Port <- 587
            smtp.EnableSsl <- true
            smtp.UseDefaultCredentials <- true
            smtp.DeliveryMethod <- SmtpDeliveryMethod.Network
            smtp.Credentials <- NetworkCredential(senderEmail, password)
            do! smtp.SendMailAsync message |> Async.AwaitTask
        }
    
    let sendEmailAsync email = async {
        do! sendEmail email.sender.email email.sender.password email.recipients email.subject email.body
    }

    let emailToolWithRecipients senderMailAddr senderMailPwd (recipientAddrArr:string[]) sub body =
        let sender  = 
            { 
                email = SenderEmailAddress senderMailAddr 
                password = secureString senderMailPwd
            }
        let recipients = 
            recipientAddrArr
            |> Array.map (fun (str:string) -> 
                {
                    name = RecipientName str
                    email = RecipientEmailAddress str
                }
                )
        let email =
            { 
                sender = sender
                recipients = recipients
                subject = Subject sub
                body = Body body
            }
        
        sendEmailAsync email |> Async.RunSynchronously

    let emailTool senderMailAddr senderMailPwd (recipientAddrCommaStr:string) sub body =
        let recipientAddrArr = recipientAddrCommaStr.Split([|','|])
        emailToolWithRecipients senderMailAddr senderMailPwd recipientAddrArr sub body

module Execution =
    open Email
    open EmailType
    [<EntryPoint>]
    let main argv =
        let sender = 
            { 
                email = SenderEmailAddress "your@gmail.com"
                password = secureString "your app password (not login password)"
            }
        let recipient = {
                name = RecipientName "receiver"
                email = RecipientEmailAddress "receiver@gmail.com"
            }
            
        let email =
            { 
                sender = sender
                recipients = [| recipient |]
                subject = Subject "test"
                body = Body "test content"
            }
        
        sendEmailAsync email |> Async.RunSynchronously
        0 // return an integer exit code
