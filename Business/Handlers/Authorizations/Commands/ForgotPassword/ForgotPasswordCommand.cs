﻿using Business.BusinessAspects.Autofac;
using Business.Constants;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.NLog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Toolkit;
using DataAccess.Abstract;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Authorizations.Commands.ForgotPassword
{
    [SecuredOperation]
    public class ForgotPasswordCommand : IRequest<IResult>
    {
        public string TCKimlikNo { get; set; }
        public string Email { get; set; }

        public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, IResult>
        {

            private readonly IUserRepository _userDal;

            public ForgotPasswordCommandHandler(IUserRepository userDal)
            {
                _userDal = userDal;
            }

            /// <summary>
            /// Handler bir kategorinin var olup olmadığını doğrular
            /// eğer yoksa yeni bir kategorinin güncellenmesine izin verir.
            /// Aspectler her zaman hadler üzerinde kullanılmalıdır.
            /// Aşağıda validation, cacheremove ve log aspect örnekleri kullanılmıştır.
            /// eğer kategori başarıyla eklenmişse sadece mesaj döner.
            /// </summary>
            /// <param name="request"></param>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            //[ValidationAspect(typeof(CreateBagisciValidator), Priority = 1)]
            [CacheRemoveAspect("Get")]
            //[LogAspect(typeof(DbLogger))]
            public async Task<IResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
            {
                var isbagisciExits = await _userDal.GetAsync(u => u.TCKimlikNo == Convert.ToInt64(request.TCKimlikNo)
                && u.Email == request.Email);

                if (isbagisciExits == null)
                    return new ErrorResult(Messages.WrongCID);
                var generatedPassword = RandomPassword.CreateRandomPassword(14);
                HashingHelper.CreatePasswordHash(generatedPassword, out byte[] passwordSalt, out byte[] passwordHash);

                var user = new User
                {
                    Status = true,
                    Email = isbagisciExits.Email,
                    CepTelefonu = isbagisciExits.CepTelefonu,
                    UserId = isbagisciExits.UserId
                };
                _userDal.Update(user);
                //TODO: Yeni Şifre SMS ya da Mail Atılsın
                return new SuccessResult(Messages.SendPassword + " Yeni Parola:" + generatedPassword + "passwordHash" + passwordHash + "passwordSalt" + passwordSalt);
            }
        }
    }
}
