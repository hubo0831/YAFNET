/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2016 Ingo Herbote
 * http://www.yetanotherforum.net/
 * 
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at

 * http://www.apache.org/licenses/LICENSE-2.0

 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

namespace YAF
{
    using System;
    using System.Text;
    using System.Web;
    using System.Web.UI;

    using YAF.Core;
    using YAF.Core.Services.Auth;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Utils;

    /// <summary>
    /// The Twitter Authentication Page
    /// </summary>
    public partial class Auth : Page
    {
        /// <summary>
        /// The script begin tag
        /// </summary>
        private const string SCRIPTBEGINTAG = "<script type='text/javascript'>";

        /// <summary>
        /// The script end tag
        /// </summary>
        private const string SCRIPTENDTAG = "</script>";

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            var closeScript = new StringBuilder();

            closeScript.Append("if (window.opener != null)");
            closeScript.AppendFormat(
                "{{window.opener.location.href = '{0}';window.close();}}",
                YafBuildLink.GetLink(ForumPages.login).Replace("auth.aspx", "default.aspx"));
            closeScript.AppendFormat(
                "else {{ window.location.href = '{0}' }}",
                YafBuildLink.GetLink(ForumPages.login).Replace("auth.aspx", "default.aspx"));

            if (this.Request.QueryString.GetFirstOrDefault("denied") != null)
            {
                this.Response.Clear();

                this.Response.Write("{0} {1} {2}".FormatWith(SCRIPTBEGINTAG, closeScript.ToString(), SCRIPTENDTAG));

                return;
            }

            var loginAuth =
                (AuthService)
                Enum.Parse(
                    typeof(AuthService),
                    YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAs<string>("auth"),
                    true);

            switch (loginAuth)
            {
                case AuthService.twitter:
                    this.HandleTwitterReturn();
                    break;
                case AuthService.facebook:
                    this.HandleFacebookReturn(); 
                    break;
                case AuthService.google:
                    this.HandleGoogleReturn();
                    break;
                default:
                    this.Response.Write("{0} {1} {2}".FormatWith(SCRIPTBEGINTAG, closeScript.ToString(), SCRIPTENDTAG));
                    break;
            }
        }

        /// <summary>
        /// Handles the twitter return.
        /// </summary>
        private void HandleTwitterReturn()
        {
            string message;

            var twitterAuth = new Twitter();

            if (YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAs<bool>("connectCurrent"))
            {
                try
                {
                    twitterAuth.ConnectUser(this.Request, null, out message);
                }
                catch (Exception ex)
                {
                    YafContext.Current.Get<ILogger>().Error(ex, "Error while trying to connect the twitter user");

                    message = ex.Message;
                }

                if (message.IsSet())
                {
                    this.Response.Write(
                        "{2} alert('{0}');window.location.href = '{1}'; {3}".FormatWith(
                            message,
                            YafBuildLink.GetLink(ForumPages.login).Replace("auth.aspx", "default.aspx"),
                            SCRIPTBEGINTAG,
                            SCRIPTENDTAG));
                }
                else
                {
                    YafBuildLink.Redirect(ForumPages.forum);
                }
            }
            else
            {
                try
                {
                    twitterAuth.LoginOrCreateUser(this.Request, null, out message);
                }
                catch (Exception ex)
                {
                    YafContext.Current.Get<ILogger>()
                        .Error(ex, "Error while trying to login or register the twitter user");

                    message = ex.Message;
                }

                this.Response.Clear();

                if (message.IsSet())
                {
                    this.Response.Write(
                        "{2} alert('{0}');window.opener.location.href = '{1}';window.close(); {3}>".FormatWith(
                            message,
                            YafBuildLink.GetLink(ForumPages.login).Replace("auth.aspx", "default.aspx"),
                            SCRIPTBEGINTAG,
                            SCRIPTENDTAG));
                }
                else
                {
                    this.Response.Write(
                        "{1} window.opener.location.href = '{0}';window.close(); {2}>".FormatWith(
                            YafBuildLink.GetLink(ForumPages.forum).Replace("auth.aspx", "default.aspx"),
                            SCRIPTBEGINTAG,
                            SCRIPTENDTAG));
                }
            }
        }

        /// <summary>
        /// Handles the facebook return.
        /// </summary>
        private void HandleFacebookReturn()
        {
            var facebookAuth = new Facebook();

            if (YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("code") != null)
            {
                var authorizationCode = YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("code");
                var accessToken = facebookAuth.GetAccessToken(authorizationCode, this.Request);

                if (accessToken.IsNotSet())
                {
                    this.Response.Write(
                        "{2} alert('{0}');window.location.href = '{1}'; {3}".FormatWith(
                            YafContext.Current.Get<ILocalization>().GetText("AUTH_NO_ACCESS_TOKEN"),
                            YafBuildLink.GetLink(ForumPages.login).Replace("auth.aspx", "default.aspx"),
                            SCRIPTBEGINTAG,
                            SCRIPTENDTAG));

                    return;
                }

                if (YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("state") != null
                    && YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("state")
                    == "connectCurrent")
                {
                    string message;

                    try
                    {
                        facebookAuth.ConnectUser(this.Request, accessToken, out message);
                    }
                    catch (Exception ex)
                    {
                        YafContext.Current.Get<ILogger>().Error(ex, "Error while trying to connect the facebook user");

                        message = ex.Message;
                    }

                    if (message.IsSet())
                    {
                        this.Response.Write(
                            "{2} alert('{0}');window.location.href = '{1}'; {3}".FormatWith(
                                message,
                                YafBuildLink.GetLink(ForumPages.forum).Replace("auth.aspx", "default.aspx"),
                                SCRIPTBEGINTAG,
                                SCRIPTENDTAG));
                    }
                    else
                    {
                        YafBuildLink.Redirect(ForumPages.forum);
                    }
                }
                else
                {
                    string message;

                    try
                    {
                        facebookAuth.LoginOrCreateUser(this.Request, accessToken, out message);
                    }
                    catch (Exception ex)
                    {
                        YafContext.Current.Get<ILogger>()
                            .Error(ex, "Error while trying to login or register the facebook user");

                        message = ex.Message;
                    }

                    this.Response.Clear();

                    if (message.IsSet())
                    {
                        this.Response.Write(
                            "{2} alert('{0}');window.location.href = '{1}';window.close(); {3}".FormatWith(
                                message,
                                YafBuildLink.GetLink(ForumPages.login).Replace("auth.aspx", "default.aspx"),
                                SCRIPTBEGINTAG,
                                SCRIPTENDTAG));
                    }
                    else
                    {
                        this.Response.Write(
                            "{1} window.location.href = '{0}';window.close(); {2}".FormatWith(
                                YafBuildLink.GetLink(ForumPages.forum).Replace("auth.aspx", "default.aspx"),
                                SCRIPTBEGINTAG,
                                SCRIPTENDTAG));
                    }
                }
            }
            else if (YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("error") != null)
            {
                // Return to login page if user cancels social login
                this.Response.Redirect(YafBuildLink.GetLink(ForumPages.login, true));
            }
            else
            {
                // Authorize first
                this.Response.Redirect(facebookAuth.GetAuthorizeUrl(this.Request), true);
            }
        }

        /// <summary>
        /// Handles the Google return.
        /// </summary>
        private void HandleGoogleReturn()
        {
            var googleAuth = new Google();

            if (YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("code") != null)
            {
                var authorizationCode = YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("code");
                var accessTokens = googleAuth.GetAccessToken(authorizationCode, this.Request);

                if (accessTokens.AccessToken == null)
                {
                    this.Response.Write(
                        "{2} alert('{0}');window.location.href = '{1}'; {3}".FormatWith(
                            YafContext.Current.Get<ILocalization>().GetText("AUTH_NO_ACCESS_TOKEN"),
                            YafBuildLink.GetLink(ForumPages.login).Replace("auth.aspx", "default.aspx"),
                            SCRIPTBEGINTAG,
                            SCRIPTENDTAG));

                    return;
                }

                if (YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAs<bool>("connectCurrent"))
                {
                    string message;

                    try
                    {
                        googleAuth.ConnectUser(this.Request, accessTokens.AccessToken, out message);
                    }
                    catch (Exception ex)
                    {
                        YafContext.Current.Get<ILogger>().Error(ex, "Error while trying to connect the facebook user");

                        message = ex.Message;
                    }

                    if (message.IsSet())
                    {
                        this.Response.Write(
                            "{2} alert('{0}');window.location.href = '{1}'; {3}".FormatWith(
                                message,
                                YafBuildLink.GetLink(ForumPages.forum).Replace("auth.aspx", "default.aspx"),
                                SCRIPTBEGINTAG,
                                SCRIPTENDTAG));
                    }
                    else
                    {
                        YafBuildLink.Redirect(ForumPages.forum);
                    }
                }
                else
                {
                    string message;

                    try
                    {
                        googleAuth.LoginOrCreateUser(this.Request, accessTokens.AccessToken, out message);
                    }
                    catch (Exception ex)
                    {
                        YafContext.Current.Get<ILogger>()
                            .Error(ex, "Error while trying to login or register the facebook user");

                        message = ex.Message;
                    }

                    this.Response.Clear();

                    if (message.IsSet())
                    {
                        this.Response.Write(
                            "{2} alert('{0}');window.location.href = '{1}';window.close(); {3}".FormatWith(
                                message,
                                YafBuildLink.GetLink(ForumPages.login).Replace("auth.aspx", "default.aspx"),
                                SCRIPTBEGINTAG,
                                SCRIPTENDTAG));
                    }
                    else
                    {
                        this.Response.Write(
                            "{1} window.location.href = '{0}';window.close(); {2}".FormatWith(
                                YafBuildLink.GetLink(ForumPages.forum).Replace("auth.aspx", "default.aspx"),
                                SCRIPTBEGINTAG,
                                SCRIPTENDTAG));
                    }
                }
            }
            else if (YafContext.Current.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("error") != null)
            {
                // Return to login page if user cancels social login
                this.Response.Redirect(YafBuildLink.GetLink(ForumPages.login, true));
            }
            else
            {
                // Authorize first
                this.Response.Redirect(googleAuth.GetAuthorizeUrl(this.Request), true);
            }
        }
    }
}