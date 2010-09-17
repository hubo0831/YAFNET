/* Yet Another Forum.net
 * Copyright (C) 2006-2010 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

namespace YAF.controls
{
  // YAF.Pages
  #region Using

  using System;
  using System.Collections;
  using System.Data;
  using System.Linq;
  using System.Text;
  using System.Web;
  using System.Web.UI.HtmlControls;
  using System.Web.UI.WebControls;

  using YAF.Classes;
  using YAF.Classes.Core;
  using YAF.Classes.Data;
  using YAF.Classes.Utils;
  using YAF.Controls;

  #endregion

  /// <summary>
  /// PollList Class
  /// </summary>
  public partial class PollList : BaseUserControl
  {
    #region Constants and Fields

    /// <summary>
    ///   The _canChange.
    /// </summary>
    private bool _canChange;

    /// <summary>
    ///   The _canVote. Used to store data from parent repeater.
    /// </summary>
    private bool _canVote;

    /// <summary>
    ///   The _data bound.
    /// </summary>
    private bool _dataBound;

    /// <summary>
    ///   The _dt poll.
    /// </summary>
    private DataTable _dtPoll;

    /// <summary>
    ///   The _dt PollGroup.
    /// </summary>
    private DataTable _dtPollGroup;

    /// <summary>
    ///   The _dt Votes.
    /// </summary>
    private DataTable _dtVotes;

    /// <summary>
    ///   The _showResults. Used to store data from parent repeater.
    /// </summary>
    private bool _showResults;

    /// <summary>
    ///   The isBound.
    /// </summary>
    private bool isBound;

    /// <summary>
    ///   The isClosedBound.
    /// </summary>
    private bool isClosedBound;

    /// <summary>
    ///   The topic User.
    /// </summary>
    private int? topicUser;

    #endregion

    #region Properties

    /// <summary>
    ///   Returns BoardId
    /// </summary>
    public int BoardId { get; set; }

    /// <summary>
    ///   Returns CategoryId
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    ///   Returns EditBoardId.
    ///   Used to return to edit board page.
    ///   Currently is not implemented.
    /// </summary>
    public int EditBoardId { get; set; }

    /// <summary>
    ///   Returns EditCategoryId
    /// </summary>
    public int EditCategoryId { get; set; }

    /// <summary>
    ///   Returns EditForumId
    /// </summary>
    public int EditForumId { get; set; }

    /// <summary>
    ///   Returns EditMessageId.
    /// </summary>
    public int EditMessageId { get; set; }

    /// <summary>
    ///   Returns ForumId
    /// </summary>
    public int ForumId { get; set; }

    /// <summary>
    ///   Returns IsLocked
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    ///   Returns MaxImageAspect. Stores max aspect to get rows of equal height.
    /// </summary>
    public decimal MaxImageAspect { get; set; }

    /// <summary>
    ///   Returns PollGroupID
    /// </summary>
    public int? PollGroupId { get; set; }

    /// <summary>
    ///   Returns PollNumber
    /// </summary>
    public int PollNumber { get; set; }

    /// <summary>
    ///   Returns If we are editing a post
    /// </summary>
    public bool PostEdit { get; set; }

    /// <summary>
    ///   Returns ShowButtons
    /// </summary>
    public bool ShowButtons { get; set; }

    /// <summary>
    ///   Returns TopicId
    /// </summary>
    public int TopicId { get; set; }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Get Theme Contents
    /// </summary>
    /// <param name="page">
    /// The Page
    /// </param>
    /// <param name="tag">
    /// Tag
    /// </param>
    /// <returns>
    /// Content
    /// </returns>
    protected string GetThemeContents(string page, string tag)
    {
      return this.PageContext.Theme.GetItem(page, tag);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Checks if a user can create poll.
    /// </summary>
    /// <returns>
    /// The can create poll.
    /// </returns>
    protected bool CanCreatePoll()
    {
      return (this.PollNumber < this.PageContext.BoardSettings.AllowedPollNumber) &&
             (this.PageContext.BoardSettings.AllowedPollChoiceNumber > 0) && this.HasOwnerExistingGroupAccess() &&
             (this.PollGroupId >= 0);
    }

    /// <summary>
    /// Checks if user can edit a poll
    /// </summary>
    /// <param name="pollId">
    /// </param>
    /// <returns>
    /// The can edit poll.
    /// </returns>
    protected bool CanEditPoll(object pollId)
    {
      if (!this.PageContext.BoardSettings.AllowPollChangesAfterFirstVote)
      {
        return this.ShowButtons &&
               (this.PageContext.IsAdmin || this.PageContext.IsForumModerator ||
                (this.PageContext.PageUserID == Convert.ToInt32(this._dtPollGroup.Rows[0]["GroupUserID"]) &&
                 this.PollHasNoVotes(pollId) || (!this.IsPollClosed(pollId))));
      }

      return this.ShowButtons &&
             (this.PageContext.IsAdmin || this.PageContext.IsForumModerator ||
              this.PageContext.PageUserID == Convert.ToInt32(this._dtPollGroup.Rows[0]["GroupUserID"]) &&
              (!this.IsPollClosed(pollId)));
    }

    /// <summary>
    /// Checks if a user can remove all polls in a group
    /// </summary>
    /// <returns>
    /// The can remove group.
    /// </returns>
    protected bool CanRemoveGroup()
    {
      bool hasNoVotes = true;

      foreach (DataRow dr in this._dtPoll.Rows)
      {
        if (Convert.ToInt32(dr["Votes"]) > 0)
        {
          hasNoVotes = false;
        }
      }

      if (!this.PageContext.BoardSettings.AllowPollChangesAfterFirstVote)
      {
        return this.ShowButtons &&
               (this.PageContext.IsAdmin || this.PageContext.IsForumModerator ||
                (this.PageContext.PageUserID == Convert.ToInt32(this._dtPollGroup.Rows[0]["GroupUserID"]) && hasNoVotes));
      }

      return this.ShowButtons &&
             (this.PageContext.IsAdmin || this.PageContext.IsForumModerator ||
              (this.PageContext.PageUserID == Convert.ToInt32(this._dtPollGroup.Rows[0]["GroupUserID"])));
    }

    /// <summary>
    /// Checks if  a user can remove all polls in a group completely.
    /// </summary>
    /// <returns>
    /// The can remove group completely.
    /// </returns>
    protected bool CanRemoveGroupCompletely()
    {
      bool hasNoVotes = true;
      foreach (DataRow dr in this._dtPoll.Rows)
      {
        if (Convert.ToInt32(dr["Votes"]) > 0)
        {
          hasNoVotes = false;
        }
      }

      if (!this.PageContext.BoardSettings.AllowPollChangesAfterFirstVote)
      {
        return this.ShowButtons &&
               (this.PageContext.IsAdmin ||
                (this.PageContext.PageUserID == Convert.ToInt32(this._dtPollGroup.Rows[0]["GroupUserID"]) && hasNoVotes));
      }

      return this.ShowButtons &&
             (this.PageContext.IsAdmin ||
              this.PageContext.PageUserID == Convert.ToInt32(this._dtPollGroup.Rows[0]["GroupUserID"]));
    }

    /// <summary>
    /// Checks if a user can delete group from all places but not completely
    /// </summary>
    /// <returns>
    /// The can remove group everywhere.
    /// </returns>
    protected bool CanRemoveGroupEverywhere()
    {
      return this.ShowButtons && this.PageContext.IsAdmin;
    }

    /// <summary>
    /// Checks if a user can delete poll without deleting it from database
    /// </summary>
    /// <param name="pollId">
    /// </param>
    /// <returns>
    /// The can remove poll.
    /// </returns>
    protected bool CanRemovePoll(object pollId)
    {
      return this.ShowButtons &&
             (this.PageContext.IsAdmin || this.PageContext.IsForumModerator ||
              (this.PageContext.PageUserID == Convert.ToInt32(this._dtPollGroup.Rows[0]["GroupUserID"])));
    }

    /// <summary>
    /// Checks if a user can delete poll completely
    /// </summary>
    /// <param name="pollId">
    /// </param>
    /// <returns>
    /// The can remove poll completely.
    /// </returns>
    protected bool CanRemovePollCompletely(object pollId)
    {
      if (!this.PageContext.BoardSettings.AllowPollChangesAfterFirstVote)
      {
        return this.ShowButtons &&
               (this.PageContext.IsAdmin || this.PageContext.IsModerator ||
                (this.PageContext.PageUserID == Convert.ToInt32(this._dtPollGroup.Rows[0]["GroupUserID"]) &&
                  this.PollHasNoVotes(pollId)));
      }

      return this.PollHasNoVotes(pollId) && this.ShowButtons &&
             (this.PageContext.IsAdmin ||
              this.PageContext.PageUserID == Convert.ToInt32(this._dtPollGroup.Rows[0]["GroupUserID"]));
    }

    /// <summary>
    /// Property to verify if the current user can vote in this poll.
    /// </summary>
    /// <param name="pollId">
    /// The poll Id.
    /// </param>
    /// <returns>
    /// The can vote.
    /// </returns>
    protected bool CanVote(object pollId)
    {
      // rule out users without voting rights
      // If you come here from topics or edit TopicId should be > 0
      if (!this.PageContext.ForumVoteAccess && this.TopicId > 0)
      {
        return false;
      }

      if (!this.PageContext.BoardVoteAccess && this.BoardId > 0)
      {
        return false;
      }

      if (this.IsPollClosed(pollId))
      {
        return false;
      }

      return this.IsNotVoted(pollId);
    }

    /// <summary>
    /// The change poll show status.
    /// </summary>
    /// <param name="newStatus">
    /// The new status.
    /// </param>
    protected void ChangePollShowStatus(bool newStatus)
    {
      /*  var pollRow = (HtmlTableRow)FindControl(String.Format("PollRow{0}", 1));

            if (pollRow != null)
            {
                pollRow.Visible = newStatus;
            }*/
    }

    /// <summary>
    /// The days to run.
    /// </summary>
    /// <param name="pollId">
    /// The poll Id.
    /// </param>
    /// <param name="soon">
    /// The soon.
    /// </param>
    /// <returns>
    /// The days to run.
    /// </returns>
    protected int? DaysToRun(object pollId, out bool soon)
    {
      soon = false;
      foreach (DataRow dr in this._dtPollGroup.Rows)
      {
        if (dr["Closes"] != DBNull.Value && Convert.ToInt32(pollId) == Convert.ToInt32(dr["PollID"]))
        {
          DateTime tCloses = Convert.ToDateTime(dr["Closes"]).Date;
          if (tCloses > DateTime.UtcNow.Date)
          {
            int days = (tCloses - DateTime.UtcNow).Days;
            // The days should not be = 0 we return whole day and add a value to 
            // say that it's a partial day
              if (days == 0)
              {
                  soon = true;
                  return 1;
              }
              return days;
          }

          return 0;
        }
      }

      return null;
    }

    /// <summary>
    /// The get image height.
    /// </summary>
    /// <param name="mimeType">
    /// The mime type.
    /// </param>
    /// <returns>
    /// The get image height.
    /// </returns>
    protected int GetImageHeight(object mimeType)
    {
      string[] attrs = mimeType.ToString().Split('!')[1].Split(';');
      return Convert.ToInt32(attrs[1]);
    }

    /// <summary>
    /// The get poll is closed.
    /// </summary>
    /// <param name="pollId">
    /// The poll Id.
    /// </param>
    /// <returns>
    /// The get poll is closed.
    /// </returns>
    protected string GetPollIsClosed(object pollId)
    {
      string strPollClosed = string.Empty;
      if (this.IsPollClosed(pollId))
      {
        strPollClosed = this.PageContext.Localization.GetText("POLL_CLOSED");
      }

      return strPollClosed;
    }

    /// <summary>
    /// The get poll question.
    /// </summary>
    /// <param name="pollId">
    /// The poll Id.
    /// </param>
    /// <returns>
    /// The get poll question.
    /// </returns>
    protected string GetPollQuestion(object pollId)
    {
      foreach (DataRow dr in this._dtPollGroup.Rows)
      {
        if (Convert.ToInt32(pollId) == Convert.ToInt32(dr["PollID"]))
        {
          return this.HtmlEncode(YafServices.BadWordReplace.Replace(dr["Question"].ToString()));
        }
      }

      return string.Empty;
    }

    /// <summary>
    /// The get total.
    /// </summary>
    /// <param name="pollId">
    /// The poll Id.
    /// </param>
    /// <returns>
    /// The get total.
    /// </returns>
    protected string GetTotal(object pollId)
    {
      foreach (DataRow dr in this._dtPollGroup.Rows)
      {
        if (Convert.ToInt32(pollId) == Convert.ToInt32(dr["PollID"]))
        {
          return this.HtmlEncode(dr["Total"].ToString());
        }
      }

      return string.Empty;
    }

    /// <summary>
    /// The is not voted.
    /// </summary>
    /// <param name="pollId">
    /// The poll id.
    /// </param>
    /// <returns>
    /// The is not voted.
    /// </returns>
    protected bool IsNotVoted(object pollId)
    {
      // check for voting cookie
      if (this.Request.Cookies[this.VotingCookieName(Convert.ToInt32(pollId))] != null)
      {
        return false;
      }

      // voting is not tied to IP and they are a guest...
      if (this.PageContext.IsGuest && !this.PageContext.BoardSettings.PollVoteTiedToIP)
      {
        return true;
      }

      // Check if a user already voted
      return this._dtVotes.Rows.Cast<DataRow>().All(dr => Convert.ToInt32(dr["PollID"]) != Convert.ToInt32(pollId));
    }

    /// <summary>
    /// The is poll closed.
    /// </summary>
    /// <param name="pollId">
    /// The poll Id.
    /// </param>
    /// <returns>
    /// The is poll closed.
    /// </returns>
    protected bool IsPollClosed(object pollId)
    {
      return (from DataRow dr in this._dtPollGroup.Rows
              where (!dr["Closes"].IsNullOrEmptyDBField()) && (Convert.ToInt32(pollId) == Convert.ToInt32(dr["PollID"]))
              select Convert.ToDateTime(dr["Closes"])).Any(tCloses => tCloses < DateTime.UtcNow);
    }

    /// <summary>
    /// Page_Load
    /// </summary>
    /// <param name="sender">
    /// </param>
    /// <param name="e">
    /// </param>
    protected void Page_Load(object sender, EventArgs e)
    {
      // if (!IsPostBack)
      // {
      if (this.TopicId > 0)
      {
        this.topicUser = Convert.ToInt32(DB.topic_info(this.TopicId)["UserID"]);
      }

      bool existingPoll = (this.PollGroupId > 0) && ((this.TopicId > 0) || (this.ForumId > 0) || (this.BoardId > 0));

      bool topicPoll = this.PageContext.ForumPollAccess &&
                       (this.EditMessageId > 0 || (this.TopicId > 0 && this.ShowButtons));
      bool forumPoll = this.EditForumId > 0 || (this.ForumId > 0 && this.ShowButtons);
      bool categoryPoll = this.EditCategoryId > 0 || (this.CategoryId > 0 && this.ShowButtons);
      bool boardPoll = this.PageContext.BoardVoteAccess &&
                       (this.EditBoardId > 0 || (this.BoardId > 0 && this.ShowButtons));

      this.NewPollRow.Visible = this.ShowButtons && this.HasOwnerExistingGroupAccess() && (!existingPoll) &&
                                (topicPoll || forumPoll || categoryPoll || boardPoll);
      if (this.PollGroupId > 0)
      {
        this.BindData();
      }
      else
      {
        if (this.NewPollRow.Visible)
        {
          this.BindCreateNewPollRow();
        }
      }

      // }
    }

    /// <summary>
    /// PollGroup_ItemCommand
    /// </summary>
    /// <param name="source">
    /// </param>
    /// <param name="e">
    /// </param>
    protected void PollGroup_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
      if (e.CommandName == "new" && this.PageContext.ForumVoteAccess)
      {
        YafBuildLink.Redirect(ForumPages.polledit, "{0}", this.ParamsToSend());
      }

      if (e.CommandName == "edit" && this.PageContext.ForumVoteAccess)
      {
        YafBuildLink.Redirect(ForumPages.polledit, "{0}&p={1}", this.ParamsToSend(), e.CommandArgument.ToString());
      }

      if (e.CommandName == "remove" && this.PageContext.ForumVoteAccess)
      {
        // ChangePollShowStatus(false);

        if (e.CommandArgument != null && e.CommandArgument.ToString() != string.Empty)
        {
          DB.poll_remove(this.PollGroupId, e.CommandArgument, this.BoardId, false, false);
          this.ReturnToPage();
          // BindData();
        }
      }

      if (e.CommandName == "removeall" && this.PageContext.ForumVoteAccess)
      {
        if (e.CommandArgument != null && e.CommandArgument.ToString() != string.Empty)
        {
          DB.poll_remove(this.PollGroupId, e.CommandArgument, this.BoardId, true, false);
          this.ReturnToPage();

          // BindData();
        }
      }

      if (e.CommandName == "removegroup" && this.PageContext.ForumVoteAccess)
      {
        DB.pollgroup_remove(this.PollGroupId, this.TopicId, this.ForumId, this.CategoryId, this.BoardId, false, false);
        this.ReturnToPage();

        // BindData();
      }

      if (e.CommandName == "removegroupall" && this.PageContext.ForumVoteAccess)
      {
        DB.pollgroup_remove(this.PollGroupId, this.TopicId, this.ForumId, this.CategoryId, this.BoardId, true, false);
        this.ReturnToPage();

        // BindData();
      }

      if (e.CommandName == "removegroupevery" && this.PageContext.ForumVoteAccess)
      {
        DB.pollgroup_remove(this.PollGroupId, this.TopicId, this.ForumId, this.CategoryId, this.BoardId, false, true);
        this.ReturnToPage();

        // BindData();
      }
    }

    /// <summary>
    /// The PollGroup item command.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void PollGroup_OnItemDataBound(object source, RepeaterItemEventArgs e)
    {
      RepeaterItem item = e.Item;
      var drowv = (DataRowView)e.Item.DataItem;
      if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
      {
        // clear the value after choiced are bounded
        this.MaxImageAspect = 1;
        item.FindControlRecursiveAs<HtmlTableRow>("PollCommandRow").Visible = this.HasOwnerExistingGroupAccess() &&
                                                                              this.ShowButtons;

        var polloll = item.FindControlRecursiveAs<Repeater>("Poll");

        string pollId = drowv.Row["PollID"].ToString();
        polloll.Visible = !this.CanVote(pollId) && !this.PageContext.BoardSettings.AllowGuestsViewPollOptions &&
                          this.PageContext.IsGuest
                            ? false
                            : true;

        // Poll Choice image
        var questionImage = item.FindControlRecursiveAs<HtmlImage>("QuestionImage");
        var questionAnchor = item.FindControlRecursiveAs<HtmlAnchor>("QuestionAnchor");

        // Don't render if it's a standard image
        if (!drowv.Row["QuestionObjectPath"].IsNullOrEmptyDBField())
        {
          questionAnchor.Attributes["rel"] = "lightbox-group" + Guid.NewGuid().ToString().Substring(0, 5);
          questionAnchor.HRef = drowv.Row["QuestionObjectPath"].IsNullOrEmptyDBField()
                                  ? this.GetThemeContents("VOTE", "POLL_CHOICE")
                                  : this.HtmlEncode(drowv.Row["QuestionObjectPath"].ToString());
          questionAnchor.Title = this.HtmlEncode(drowv.Row["QuestionObjectPath"].ToString());

          questionImage.Src = questionImage.Alt = this.HtmlEncode(drowv.Row["QuestionObjectPath"].ToString());
         
          if (!drowv.Row["QuestionMimeType"].IsNullOrEmptyDBField())
          {
            decimal aspect = GetImageAspect(drowv.Row["QuestionMimeType"]);

            // hardcoded - bad
            questionImage.Width = 80;
            questionImage.Height = Convert.ToInt32(questionImage.Width / aspect);
          }
        }
        else
        {
          questionImage.Alt = this.PageContext.Localization.GetText("POLLEDIT", "POLL_PLEASEVOTE");
          questionImage.Src = this.GetThemeContents("VOTE", "POLL_QUESTION");
          questionAnchor.HRef = string.Empty;
        }

        DataTable _choiceRow = this._dtPoll.Copy();
        foreach (DataRow drr in _choiceRow.Rows)
        {
          if (Convert.ToInt32(drr["PollID"]) != Convert.ToInt32(pollId))
          {
            drr.Delete();
          }
          else
          {
            if (!drr["MimeType"].IsNullOrEmptyDBField())
            {
              decimal currentAspect = GetImageAspect(drr["MimeType"]);
              if (currentAspect > this.MaxImageAspect)
              {
                this.MaxImageAspect = currentAspect;
              }
            }
          }
        }

        polloll.DataSource = _choiceRow;
        this._canVote = this.CanVote(pollId);
        bool isPollClosed = this.IsPollClosed(pollId);
        bool isNotVoted = this.IsNotVoted(pollId);

        // Poll voting is bounded - you can't see results before voting in each poll
        if (this.isBound)
        {
          int voteCount =
            this._dtPollGroup.Rows.Cast<DataRow>().Count(
              dr => !this.IsNotVoted(dr["PollID"]) && !this.IsPollClosed(dr["PollID"]));

          if (!isPollClosed && voteCount >= this.PollNumber)
          {
            this._showResults = true;
          }
        }
        else
        {
          if (!this.isClosedBound && this.PageContext.BoardSettings.AllowUsersViewPollVotesBefore)
          {
            this._showResults = true;
          }
        }

        // The poll expired. We show results  
        if (this.isClosedBound && isPollClosed)
        {
          this._showResults = true;
        }

        polloll.DataBind();

        // Clear the fields after the child repeater is bound
        this._showResults = false;
        this._canVote = false;

        // Add confirmations to delete buttons
        var removePollAll = item.FindControlRecursiveAs<ThemeButton>("RemovePollAll");
        removePollAll.Attributes["onclick"] =
          "return confirm('{0}');".FormatWith(this.PageContext.Localization.GetText("POLLEDIT", "ASK_POLL_DELETE_ALL"));
        removePollAll.Visible = this.CanRemovePollCompletely(pollId);

        var removePoll = item.FindControlRecursiveAs<ThemeButton>("RemovePoll");
        removePoll.Attributes["onclick"] =
          "return confirm('{0}');".FormatWith(this.PageContext.Localization.GetText("POLLEDIT", "ASK_POLL_DELETE"));
        removePoll.Visible = this.CanRemovePoll(pollId);

        // Poll warnings section
        bool soon;
        bool showWarningsRow = false;

        // returns number of day to run - null if nothing 
        int? daystorun = this.DaysToRun(pollId, out soon);

        var pollVotesLabel = item.FindControlRecursiveAs<Label>("PollVotesLabel");
        bool cvote = this.CanVote(pollId);
        if (cvote)
        {
          if (this.isBound && this.PollNumber > 1 && this.PollNumber >= this._dtVotes.Rows.Count)
          {
            pollVotesLabel.Text = this.PageContext.Localization.GetText("POLLEDIT", "POLLGROUP_BOUNDWARN");
          }

          if (!this.PageContext.BoardSettings.AllowUsersViewPollVotesBefore)
          {
            if (!this.PageContext.IsGuest)
            {
              pollVotesLabel.Text += this.PageContext.Localization.GetText("POLLEDIT", "POLLRESULTSHIDDEN");
            }
            else
            {
              pollVotesLabel.Text += this.PageContext.Localization.GetText("POLLEDIT", "POLLRESULTSHIDDEN_GUEST");
            }
          }
        }

        if (this.PageContext.IsGuest)
        {
          var guestOptionsHidden = item.FindControlRecursiveAs<Label>("GuestOptionsHidden");
            if (!cvote)
            {
                if (!this.PageContext.BoardSettings.AllowGuestsViewPollOptions)
                {
                    guestOptionsHidden.Text = this.PageContext.Localization.GetText("POLLEDIT", "POLLOPTIONSHIDDEN_GUEST");
                    guestOptionsHidden.Visible = true;
                    showWarningsRow = true;
                }
                if (isNotVoted)
                {
                    guestOptionsHidden.Text += this.PageContext.Localization.GetText("POLLEDIT", "POLL_NOPERM_GUEST");
                    guestOptionsHidden.Visible = true;
                    showWarningsRow = true;
                }
                
          }
        }

        pollVotesLabel.Visible = this.isBound ||
                                 (this.PageContext.BoardSettings.AllowUsersViewPollVotesBefore
                                    ? false
                                    : (isNotVoted || (daystorun == null)));
        if (pollVotesLabel.Visible)
        {
          showWarningsRow = true;
        }

        if (!isNotVoted &&
            (this.PageContext.ForumVoteAccess ||
             (this.PageContext.BoardVoteAccess && (this.BoardId > 0 || this.EditBoardId > 0))))
        {
          var alreadyVotedLabel = item.FindControlRecursiveAs<Label>("AlreadyVotedLabel");
          alreadyVotedLabel.Text = this.PageContext.Localization.GetText("POLLEDIT", "POLL_VOTED");
          showWarningsRow = alreadyVotedLabel.Visible = true;
        }

        // Poll has expiration date
        if (daystorun != null)
        {
          var pollWillExpire = item.FindControlRecursiveAs<Label>("PollWillExpire");
          if (!soon)
          {
            pollWillExpire.Text = this.PageContext.Localization.GetTextFormatted("POLL_WILLEXPIRE", daystorun);
          }
          else
          {
            pollWillExpire.Text = this.PageContext.Localization.GetText("POLLEDIT", "POLL_WILLEXPIRE_HOURS");
          }

          showWarningsRow = pollWillExpire.Visible = true;
        }
        else if (daystorun == 0)
        {
          var pollExpired = item.FindControlRecursiveAs<Label>("PollExpired");
          pollExpired.Text = this.PageContext.Localization.GetText("POLLEDIT", "POLL_EXPIRED");
          showWarningsRow = pollExpired.Visible = true;
        }

        item.FindControlRecursiveAs<HtmlTableRow>("PollInfoTr").Visible = showWarningsRow;

        DisplayButtons();
      }

      // Populate PollGroup Repeater footer
      if (item.ItemType == ListItemType.Footer)
      {
        var pgcr = item.FindControlRecursiveAs<HtmlTableRow>("PollGroupCommandRow");
        pgcr.Visible = this.HasOwnerExistingGroupAccess() && this.ShowButtons;
        
        // return confirmations for poll group 
        if (pgcr.Visible)
        {
          item.FindControlRecursiveAs<ThemeButton>("RemoveGroup").Attributes["onclick"] =
            "return confirm('{0}');".FormatWith(
              this.PageContext.Localization.GetText("POLLEDIT", "ASK_POLLGROUP_DELETE"));

          item.FindControlRecursiveAs<ThemeButton>("RemoveGroupAll").Attributes["onclick"] =
            "return confirm('{0}');".FormatWith(
              this.PageContext.Localization.GetText("POLLEDIT", "ASK_POLLROUP_DELETE_ALL"));

          item.FindControlRecursiveAs<ThemeButton>("RemoveGroupEverywhere").Attributes["onclick"] =
            "return confirm('{0}');".FormatWith(
              this.PageContext.Localization.GetText("POLLEDIT", "ASK_POLLROUP_DELETE_EVR"));
        }
      }
    }

    /// <summary>
    /// The poll_ item command.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void Poll_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
      if (e.CommandName == "vote" && e.CommandArgument != null &&
          ((this.PageContext.ForumVoteAccess && this.TopicId > 0) ||
           (this.PageContext.BoardVoteAccess && this.BoardId > 0)))
      {
        if (!this.CanVote(Convert.ToInt32(e.CommandArgument)))
        {
          this.PageContext.AddLoadMessage(this.PageContext.Localization.GetText("WARN_ALREADY_VOTED"));
          return;
        }

        if (this.IsLocked)
        {
          this.PageContext.AddLoadMessage(this.PageContext.Localization.GetText("WARN_TOPIC_LOCKED"));
          return;
        }

        foreach (DataRow drow  in this._dtPoll.Rows)
        {
          if ((int)drow["ChoiceID"] == Convert.ToInt32(e.CommandArgument))
          {
            if (this.IsPollClosed(Convert.ToInt32(drow["PollID"])))
            {
              this.PageContext.AddLoadMessage(this.PageContext.Localization.GetText("WARN_POLL_CLOSED"));
              return;
            }

            break;
          }
        }

        object userID = null;
        object remoteIP = null;

        if (this.PageContext.BoardSettings.PollVoteTiedToIP)
        {
          remoteIP = IPHelper.IPStrToLong(this.Request.ServerVariables["REMOTE_ADDR"]).ToString();
        }

        if (!this.PageContext.IsGuest)
        {
          userID = this.PageContext.PageUserID;
        }

        DB.choice_vote(e.CommandArgument, userID, remoteIP);

        // save the voting cookie...
        var c = new HttpCookie(this.VotingCookieName(Convert.ToInt32(e.CommandArgument)), e.CommandArgument.ToString())
          {
             Expires = DateTime.UtcNow.AddYears(1) 
          };
        this.Response.Cookies.Add(c);
        string msg = this.PageContext.Localization.GetText("INFO_VOTED");

        if (this.isBound && this.PollNumber > 1 && this.PollNumber >= this._dtVotes.Rows.Count &&
            (!this.PageContext.BoardSettings.AllowUsersViewPollVotesBefore))
        {
          msg += this.PageContext.Localization.GetText("POLLGROUP_BOUNDWARN");
        }

        this.PageContext.AddLoadMessage(msg);
        this.BindData();
      }
    }

    /// <summary>
    /// The poll_ on item data bound.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void Poll_OnItemDataBound(object source, RepeaterItemEventArgs e)
    {
      RepeaterItem item = e.Item;
      var drowv = (DataRowView)e.Item.DataItem;
      var trow = item.FindControlRecursiveAs<HtmlTableRow>("VoteTr");

      if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
      {
        // Voting link 
        var myLinkButton = item.FindControlRecursiveAs<MyLinkButton>("MyLinkButton1");
        string pollId = drowv.Row["PollID"].ToString();

        myLinkButton.Enabled = this._canVote;
        myLinkButton.ToolTip = this.PageContext.Localization.GetText("POLLEDIT", "POLL_PLEASEVOTE");
        myLinkButton.Visible = true;

        // Poll Choice image
        var choiceImage = item.FindControlRecursiveAs<HtmlImage>("ChoiceImage");
        var choiceAnchor = item.FindControlRecursiveAs<HtmlAnchor>("ChoiceAnchor");

        // Don't render if it's a standard image
        if (!drowv.Row["ObjectPath"].IsNullOrEmptyDBField())
        {
          choiceAnchor.Attributes["rel"] = "lightbox-group" + Guid.NewGuid().ToString().Substring(0, 5);
          choiceAnchor.HRef = drowv.Row["ObjectPath"].IsNullOrEmptyDBField()
                                ? this.GetThemeContents("VOTE", "POLL_CHOICE")
                                : this.HtmlEncode(drowv.Row["ObjectPath"].ToString());
          choiceAnchor.Title = drowv.Row["ObjectPath"].ToString();

          choiceImage.Src = choiceImage.Alt = this.HtmlEncode(drowv.Row["ObjectPath"].ToString());
         

          if (!drowv.Row["MimeType"].IsNullOrEmptyDBField())
          {
            decimal aspect = GetImageAspect(drowv.Row["MimeType"]);

            // hardcoded - bad
            const int imageWidth = 80;
            choiceImage.Attributes["style"] = "width:{0}px; height:{1}px;".FormatWith(
              imageWidth, choiceImage.Width / aspect);

            // reserved to get equal row heights
            string height = (this.MaxImageAspect * choiceImage.Width).ToString();
            trow.Attributes["style"] = "height:{0}px;".FormatWith(height);
          }
        }
        else
        {
          choiceImage.Alt = this.PageContext.Localization.GetText("POLLEDIT", "POLL_PLEASEVOTE");
          choiceImage.Src = this.GetThemeContents("VOTE", "POLL_CHOICE");
          choiceAnchor.HRef = string.Empty;
        }

        item.FindControlRecursiveAs<Panel>("MaskSpan").Visible = !this._showResults;
        item.FindControlRecursiveAs<Panel>("resultsSpan").Visible =
          item.FindControlRecursiveAs<Panel>("VoteSpan").Visible = this._showResults;
      }
    }

    /// <summary>
    /// The remove poll_ completely load.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void RemovePollCompletely_Load(object sender, EventArgs e)
    {
      ((ThemeButton)sender).Attributes["onclick"] =
        "return confirm('{0}');".FormatWith(this.PageContext.Localization.GetText("POLLEDIT", "ASK_POLL_DELETE_ALL"));
    }

    /// <summary>
    /// The remove poll_ load.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void RemovePoll_Load(object sender, EventArgs e)
    {
      ((ThemeButton)sender).Attributes["onclick"] =
        "return confirm('{0}');".FormatWith(this.PageContext.Localization.GetText("POLLEDIT", "ASK_POLL_DELETE"));
    }

    /// <summary>
    /// The vote width.
    /// </summary>
    /// <param name="o">
    /// The o.
    /// </param>
    /// <returns>
    /// The vote width.
    /// </returns>
    protected int VoteWidth(object o)
    {
      var row = (DataRowView)o;
      return (int)row.Row["Stats"] * 80 / 100;
    }

    /// <summary>
    /// Gets VotingCookieName.
    /// </summary>
    /// <param name="pollId">
    /// The poll Id.
    /// </param>
    /// <returns>
    /// The voting cookie name.
    /// </returns>
    protected string VotingCookieName(int pollId)
    {
      return "poll#{0}".FormatWith(pollId);
    }

    /// <summary>
    /// The display buttons.
    /// </summary>
    private static void DisplayButtons()
    {
      // PollGroup.FindControlRecursiveAs<HtmlTableRow>("PollCommandRow").Visible = ShowButtons;
    }

    /// <summary>
    /// Returns an image width|height ratio.
    /// </summary>
    /// <param name="mimeType">
    /// </param>
    /// <returns>
    /// The get image aspect.
    /// </returns>
    private static decimal GetImageAspect(object mimeType)
    {
      if (!mimeType.IsNullOrEmptyDBField())
      {
        string[] attrs = mimeType.ToString().Split('!')[1].Split(';');
        decimal width = Convert.ToDecimal(attrs[0]);
        return width / Convert.ToDecimal(attrs[1]);
      }

      return 1;
    }

    /// <summary>
    /// The bind create new poll row.
    /// </summary>
    private void BindCreateNewPollRow()
    {
      var cpr = this.CreatePoll1;

      // ChangePollShowStatus(true);
      cpr.NavigateUrl = YafBuildLink.GetLinkNotEscaped(ForumPages.polledit, "{0}", this.ParamsToSend());
      cpr.DataBind();
      cpr.Visible = true;
      this.NewPollRow.Visible = true;
    }

    /// <summary>
    /// The bind data.
    /// </summary>
    private void BindData()
    {
      this._dataBound = true;
      this.PollNumber = 0;
      this._dtPoll = DB.pollgroup_stats(this.PollGroupId);

      // if the page user can cheange the poll. Only a group owner, forum moderator  or an admin can do it.   
      this._canChange = (Convert.ToInt32(this._dtPoll.Rows[0]["GroupUserID"]) == this.PageContext.PageUserID) ||
                        this.PageContext.IsAdmin || this.PageContext.IsForumModerator;

      // check if we should hide pollgroup repeater when a message is posted
      if (this.Parent.Page.ClientQueryString.Contains("postmessage"))
      {
        this.PollGroup.Visible = ((this.EditMessageId > 0) && (!this._canChange)) || this._canChange;
      }
      else
      {
        this.PollGroup.Visible = true;
      }

      this._dtPollGroup = this._dtPoll.Copy();

      // TODO: repeating code - move to Utils
      // Remove repeating PollID values   
      var hTable = new Hashtable();
      var duplicateList = new ArrayList();

      foreach (DataRow drow in this._dtPollGroup.Rows)
      {
        if (hTable.Contains(drow["PollID"]))
        {
          duplicateList.Add(drow);
        }
        else
        {
          hTable.Add(drow["PollID"], string.Empty);
        }
      }

      foreach (DataRow dRow in duplicateList)
      {
        this._dtPollGroup.Rows.Remove(dRow);
      }

      this._dtPollGroup.AcceptChanges();

      // frequently used
      this.PollNumber = this._dtPollGroup.Rows.Count;

      if (this._dtPollGroup.Rows.Count > 0)
      {
        // Check if the user is already voted in polls in the group 
        object userId = null;
        object remoteIp = null;

        if (this.PageContext.BoardSettings.PollVoteTiedToIP)
        {
          remoteIp = IPHelper.IPStrToLong(this.Request.UserHostAddress).ToString();
        }

        if (!this.PageContext.IsGuest)
        {
          userId = this.PageContext.PageUserID;
        }

        this._dtVotes = DB.pollgroup_votecheck(this.PollGroupId, userId, remoteIp);

        this.isBound = Convert.ToInt32(this._dtPollGroup.Rows[0]["IsBound"]) == 2;
        this.isClosedBound = Convert.ToInt32(this._dtPollGroup.Rows[0]["IsClosedBound"]) == 4;

        this.PollGroup.DataSource = this._dtPollGroup;

        // we hide new poll row if a poll exist
        this.NewPollRow.Visible = false;
        this.ChangePollShowStatus(true);
      }

      this.DataBind();
    }

    /// <summary>
    /// The has owner existing group access.
    /// </summary>
    /// <returns>
    /// The has owner existing group access.
    /// </returns>
    private bool HasOwnerExistingGroupAccess()
    {
      if (this.PageContext.BoardSettings.AllowedPollChoiceNumber > 0)
      {
        // if topicid > 0 it can be every member
        if (this.TopicId > 0)
        {
          return (this.topicUser == this.PageContext.PageUserID) || this.PageContext.IsAdmin ||
                 this.PageContext.IsForumModerator;
        }

        // only admins can edit this
        if (this.CategoryId > 0 || this.BoardId > 0)
        {
          return this.PageContext.IsAdmin;
        }

        // in other places only admins and forum moderators can have access
        return this.PageContext.IsAdmin || this.PageContext.IsForumModerator;
      }

      return false;
    }

    /// <summary>
    /// A method to return parameters string. 
    /// Consrtucts parameters string to send to other forms.
    /// </summary>
    /// <returns>
    /// The params to send.
    /// </returns>
    private string ParamsToSend()
    {
      String sb = string.Empty;

      if (this.TopicId > 0)
      {
          sb += "t={0}".FormatWith(this.TopicId);
      }

      if (this.EditMessageId > 0)
      {
        if (sb.IsSet())
        {
          sb += '&';
        }
          sb += "m={0}".FormatWith(this.EditMessageId);
      }

      if (this.ForumId > 0)
      {
          if (sb.IsSet())
          {
              sb += '&';
          }

        sb += "f={0}".FormatWith(this.ForumId);
      }

      if (this.EditForumId > 0)
      {
          if (sb.IsSet())
          {
              sb += '&';
          }

        sb += "ef={0}".FormatWith(this.EditForumId);
      }

      if (this.CategoryId > 0)
      {
          if (sb.IsSet())
          {
              sb += '&';
          }

        sb += "c={0}".FormatWith(this.CategoryId);
      }

      if (this.EditCategoryId > 0)
      {
          if (sb.IsSet())
          {
              sb += '&';
          }

        sb += "ec={0}".FormatWith(this.EditCategoryId);
      }

      if (this.BoardId > 0)
      {
          if (sb.IsSet())
          {
              sb += '&';
          }

        sb += "b={0}".FormatWith(this.BoardId);
      }

      if (this.EditBoardId > 0)
      {
          if (sb.IsSet())
          {
              sb += '&';
          }

        sb += "eb={0}".FormatWith(this.EditBoardId);
      }

      return sb;
    }

    /// <summary>
    /// Checks if a poll has no votes.
    /// </summary>
    /// <param name="pollId">
    /// </param>
    /// <returns>
    /// The poll has no votes.
    /// </returns>
    private bool PollHasNoVotes(object pollId)
    {
      return
        this._dtPoll.Rows.Cast<DataRow>().Where(dr => Convert.ToInt32(dr["PollID"]) == Convert.ToInt32(pollId)).All(
          dr => Convert.ToInt32(dr["Votes"]) <= 0);
    }

    /// <summary>
    /// Returns user to the original call page.
    /// </summary>
    private void ReturnToPage()
    {
      // We simply return here to the page where the control is put. It can be made other way.
      if (this.TopicId > 0)
      {
        YafBuildLink.Redirect(ForumPages.posts, "t={0}", this.TopicId);
      }

      if (this.EditMessageId > 0)
      {
        YafBuildLink.Redirect(ForumPages.postmessage, "m={0}", this.EditMessageId);
      }

      if (this.ForumId > 0)
      {
        YafBuildLink.Redirect(ForumPages.topics, "f={0}", this.ForumId);
      }

      if (this.EditForumId > 0)
      {
        YafBuildLink.Redirect(ForumPages.admin_editforum, "f={0}", this.ForumId);
      }

      if (this.CategoryId > 0)
      {
        YafBuildLink.Redirect(ForumPages.forum, "c={0}", this.CategoryId);
      }

      if (this.EditCategoryId > 0)
      {
        YafBuildLink.Redirect(ForumPages.admin_editcategory, "c={0}", this.EditCategoryId);
      }

      if (this.BoardId > 0)
      {
        YafBuildLink.Redirect(ForumPages.forum);
      }

      if (this.EditBoardId > 0)
      {
        YafBuildLink.Redirect(ForumPages.admin_editboard, "b={0}", this.EditBoardId);
      }

      YafBuildLink.RedirectInfoPage(InfoMessage.Invalid);
    }

    #endregion
  }
}