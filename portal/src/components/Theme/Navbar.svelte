<script>
  import FeatherIcon from "../FeatherIcon";
  import Toggle from '../Toggle';
  import Dropdown from '../Dropdown';
</script>

<div {...$$props} class="navbar is-inline-flex is-transparent no-shadow {$$props.class || ''}">
  <div class="container is-fluid">

    <slot name="navbar-brand">
      <div class="navbar-brand">
        <a class="navbar-item" on:click|preventDefault|stopPropagation>
          <FeatherIcon icon="hexagon" />
        </a>
      </div>
    </slot>

    <slot />

    <slot name="navbar-menu">
      <div class="navbar-menu">

        <slot name="navbar-start">
          <div class="navbar-start">
            <Dropdown class="navbar-item is-icon drop-trigger" let:active>
              <a slot="trigger" class="icon-link is-primary is-friends">
                <FeatherIcon icon="heart" />
                <span class="indicator" />
              </a>

              <div class="nav-drop" class:is-active={active}>
                <div class="inner">
                  <div class="nav-drop-header">
                    <span>Friend requests</span>
                    <a href="#">Find Friends</a>
                  </div>
                  <div class="nav-drop-body is-friend-requests is-active">
                    <!-- Friend request -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/300x300"
                            data-demo-src="assets/images/avatars/bobby.jpg"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <a href="#">Bobby Brown</a>
                        <span>Najeel verwick is a common friend</span>
                      </div>
                      <div class="media-right">
                        <button
                          class="button icon-button is-solid grey-button raised">
                          <FeatherIcon icon="user-plus" />
                        </button>
                        <button
                          class="button icon-button is-solid grey-button raised">
                          <FeatherIcon icon="user-minus" />
                        </button>
                      </div>
                    </div>
                    <!-- Friend request -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/300x300"
                            data-demo-src="assets/images/avatars/dan.jpg"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <a href="#">Dan Walker</a>
                        <span>You have 4 common friends</span>
                      </div>
                      <div class="media-right">
                        <button
                          class="button icon-button is-solid grey-button raised">
                          <FeatherIcon icon="user-plus" />
                        </button>
                        <button
                          class="button icon-button is-solid grey-button raised">
                          <FeatherIcon icon="user-minus" />
                        </button>
                      </div>
                    </div>
                    <!-- Friend request -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/300x300"
                            data-demo-src="assets/images/avatars/nelly.png"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <span>
                          You are now friends with
                          <a href="#">Nelly Schwartz</a>
                          . Check her
                          <a href="#">profile</a>
                          .
                        </span>
                      </div>
                      <div class="media-right">
                        <div class="added-icon">
                          <FeatherIcon icon="tag" />
                        </div>
                      </div>
                    </div>
                    <!-- Friend request -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/300x300"
                            data-demo-src="assets/images/avatars/milly.jpg"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <a href="#">Milly Augustine</a>
                        <span>You have 8 common friends</span>
                      </div>
                      <div class="media-right">
                        <button
                          class="button icon-button is-solid grey-button raised">
                          <FeatherIcon icon="user-plus" />
                        </button>
                        <button
                          class="button icon-button is-solid grey-button raised">
                          <FeatherIcon icon="user-minus" />
                        </button>
                      </div>
                    </div>
                    <!-- Friend request -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/300x300"
                            data-demo-src="assets/images/avatars/elise.jpg"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <span>
                          You are now friends with
                          <a href="#">Elise Walker</a>
                          . Check her
                          <a href="#">profile</a>
                          .
                        </span>
                      </div>
                      <div class="media-right">
                        <div class="added-icon">
                          <FeatherIcon icon="tag" />
                        </div>
                      </div>
                    </div>
                    <!-- Friend request -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/300x300"
                            data-demo-src="assets/images/avatars/edward.jpeg"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <span>
                          You are now friends with
                          <a href="#">Edward Mayers</a>
                          . Check his
                          <a href="#">profile</a>
                          .
                        </span>
                      </div>
                      <div class="media-right">
                        <div class="added-icon">
                          <FeatherIcon icon="tag" />
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="nav-drop-footer">
                    <a href="#">Clear All</a>
                  </div>
                </div>
              </div>
            </Dropdown>
            <div class="navbar-item is-icon drop-trigger">
              <a class="icon-link" href="javascript:void(0);">
                <FeatherIcon icon="bell" />
                <span class="indicator" />
              </a>

              <div class="nav-drop">
                <div class="inner">
                  <div class="nav-drop-header">
                    <span>Notifications</span>
                    <a href="#">View All</a>
                  </div>
                  <div class="nav-drop-body is-notifications">
                    <!-- Notification -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/300x300"
                            data-demo-src="assets/images/avatars/david.jpg"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <span>
                          <a href="#">David Kim</a>
                          commented on
                          <a href="#">your post</a>
                          .
                        </span>
                        <span class="time">30 minutes ago</span>
                      </div>
                      <div class="media-right">
                        <div class="added-icon">
                          <FeatherIcon icon="message-square" />
                        </div>
                      </div>
                    </div>
                    <!-- Notification -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/300x300"
                            data-demo-src="assets/images/avatars/daniel.jpg"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <span>
                          <a href="#">Daniel Wellington</a>
                          liked your
                          <a href="#">profile.</a>
                        </span>
                        <span class="time">43 minutes ago</span>
                      </div>
                      <div class="media-right">
                        <div class="added-icon">
                          <FeatherIcon icon="heart" />
                        </div>
                      </div>
                    </div>
                    <!-- Notification -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/300x300"
                            data-demo-src="assets/images/avatars/stella.jpg"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <span>
                          <a href="#">Stella Bergmann</a>
                          shared a
                          <a href="#">New video</a>
                          on your wall.
                        </span>
                        <span class="time">Yesterday</span>
                      </div>
                      <div class="media-right">
                        <div class="added-icon">
                          <FeatherIcon icon="youtube" />
                        </div>
                      </div>
                    </div>
                    <!-- Notification -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/300x300"
                            data-demo-src="assets/images/avatars/elise.jpg"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <span>
                          <a href="#">Elise Walker</a>
                          shared an
                          <a href="#">Image</a>
                          with you an 2 other people.
                        </span>
                        <span class="time">2 days ago</span>
                      </div>
                      <div class="media-right">
                        <div class="added-icon">
                          <FeatherIcon icon="image" />
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="nav-drop-footer">
                    <a href="#">Clear All</a>
                  </div>
                </div>
              </div>
            </div>
            <div class="navbar-item is-icon drop-trigger">
              <a class="icon-link is-active" href="javascript:void(0);">
                <FeatherIcon icon="mail" />
                <span class="indicator" />
              </a>

              <div class="nav-drop">
                <div class="inner">
                  <div class="nav-drop-header">
                    <span>Messages</span>
                    <a href="messages-inbox.html">Inbox</a>
                  </div>
                  <div class="nav-drop-body is-friend-requests">
                    <!-- Message -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/150x150"
                            data-demo-src="assets/images/avatars/nelly.png"
                            data-user-popover="9"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <a href="#">Nelly Schwartz</a>
                        <span>
                          I think we should meet near the Starbucks so we can get...
                        </span>
                        <span class="time">Yesterday</span>
                      </div>
                      <div class="media-right is-centered">
                        <div class="added-icon">
                          <FeatherIcon icon="message-square" />
                        </div>
                      </div>
                    </div>
                    <!-- Message -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/150x150"
                            data-demo-src="assets/images/avatars/edward.jpeg"
                            data-user-popover="5"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <a href="#">Edward Mayers</a>
                        <span>
                          That was a real pleasure seeing you last time we really
                          should...
                        </span>
                        <span class="time">last week</span>
                      </div>
                      <div class="media-right is-centered">
                        <div class="added-icon">
                          <FeatherIcon icon="message-square" />
                        </div>
                      </div>
                    </div>
                    <!-- Message -->
                    <div class="media">
                      <figure class="media-left">
                        <p class="image">
                          <img
                            src="https://via.placeholder.com/150x150"
                            data-demo-src="assets/images/avatars/dan.jpg"
                            data-user-popover="1"
                            alt="" />
                        </p>
                      </figure>
                      <div class="media-content">
                        <a href="#">Dan Walker</a>
                        <span>
                          Hey there, would it be possible to borrow your bicycle, i
                          really need...
                        </span>
                        <span class="time">Jun 03 2018</span>
                      </div>
                      <div class="media-right is-centered">
                        <div class="added-icon">
                          <FeatherIcon icon="message-square" />
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="nav-drop-footer">
                    <a href="#">Clear All</a>
                  </div>
                </div>
              </div>
            </div>
            <div class="navbar-item is-icon open-chat">
              <a class="icon-link is-primary" href="javascript:void(0);">
                <FeatherIcon icon="message-square" />
                <span class="indicator" />
              </a>
            </div>

          </div>
        </slot>
  
        <slot name="navbar-end">
          <div class="navbar-end">
                    
            <Dropdown class="navbar-item is-account drop-trigger has-caret" let:active>
              <div slot="trigger" class="user-image">
                <img src="//via.placeholder.com/400x400" alt="" />
                <span class="indicator" />
              </div>

              <div class="nav-drop is-account-dropdown" class:is-active={active}>
                <div class="inner">
                  <div class="nav-drop-header">
                    <span class="username">Jenna Davis</span>
                    <a href="profile-main.html">Profile</a>
                  </div>
                  <div class="nav-drop-body account-items">
                    
                    <a class="account-item">
                      <div class="media">
                        <div class="icon-wrap">
                          <FeatherIcon icon="power" />
                        </div>
                        <div class="media-content">
                          <h3>Log out</h3>
                          <small>Log out of Beamable</small>
                        </div>
                      </div>
                    </a>
                  </div>
                </div>
              </div>
            </Dropdown>

          </div>
        </slot>

    </slot>


  </div>
</div>
