using System;
using System.Threading.Tasks;
using System.Linq;

using metastrings;

namespace metascript
{
    public static class EmailTokens
    {
        public static async Task<string> CreateEmailTokenAsync(Context ctxt, string email)
        {
            WebUtils.ValidateEmail(email);
            string token = MUtils.CreateToken(64);
            var define = new Define("emailtokens", email);
            define.Set("token", token);
            await ctxt.Cmd.DefineAsync(define);
            return token;
        }

        public static async Task<string> LookupEmailTokenAsync(Context ctxt, string email)
        {
            WebUtils.ValidateEmail(email);

            var select = Sql.Parse("SELECT token FROM emailtokens WHERE value = @email");
            select.AddParam("@email", email);
            var tokenObj = await ctxt.ExecScalarAsync(select);
            if (tokenObj == null || tokenObj == DBNull.Value)
                throw new Exception("Email not found: " + email);

            await ctxt.Cmd.DeleteAsync("emailtokens", email);

            string token = tokenObj.ToString();
            return token;
        }

        public async static Task<string> ComputeSignatureAsync(HttpState state, string email, string token)
        {
            await WebUtils.LogTraceAsync(state, "ComputeSignatureAsync: {0} - {1}", email, token);
            string scrambledToken;
            {
                char[] scrambledTokenArray = (email + token).ToCharArray().Reverse().ToArray();
                for (int t = 0; t < scrambledTokenArray.Length; ++t)
                {
                    int cur = scrambledTokenArray[t];
                    int otherIdx = cur % scrambledTokenArray.Length;
                    char temp = scrambledTokenArray[t];
                    scrambledTokenArray[t] = scrambledTokenArray[otherIdx];
                    scrambledTokenArray[otherIdx] = temp;
                }
                scrambledToken = new string(scrambledTokenArray);
            }
            await WebUtils.LogTraceAsync(state, "ComputeSignatureAsync: scambled: {0}", scrambledToken);

            string signature = MUtils.HashStr(scrambledToken);
            await WebUtils.LogTraceAsync(state, "ComputeSignatureAsync: hashed: {0}", signature);
            return signature;
        }

        public static async Task ValidateRequestAsync(HttpState state, string email, string signature)
        {
            WebUtils.ValidateEmail(email);
            if (string.IsNullOrWhiteSpace(signature))
                throw new UserException("Invalid request, signature missing");
            await WebUtils.LogTraceAsync(state, "ValidateRequestAsync: {0} - {1}", email, signature);

            string token = await LookupEmailTokenAsync(state.MsCtxt, email);
            await WebUtils.LogTraceAsync(state, "ValidateRequestAsync: token: {0}", token);

            string ourSignature = await ComputeSignatureAsync(state, email, token);
            await WebUtils.LogTraceAsync(state, "ValidateRequestAsync: ours: {0}", ourSignature);

            if (ourSignature != signature)
            {
                await WebUtils.LogInfoAsync(state, $"ValidateRequestAsync: signature mismatch: {email} - theirs: {signature} - ours: {ourSignature}");
                throw new UserException("Invalid request, signatures do not match");
            }
        }
    }
}
