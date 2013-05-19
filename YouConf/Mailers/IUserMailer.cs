using Mvc.Mailer;

namespace YouConf.Mailers
{ 
    public interface IUserMailer
    {
			MvcMailMessage Welcome();
            MvcMailMessage PasswordReset(string email, string token);
	}
}