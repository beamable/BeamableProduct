<script>
  import Navbar from "./Navbar";
</script>

<slot name="navbar">
  <Navbar />
</slot>

<div class="view-wrapper">
  <div class="is-smaller">
    <div class="container">
      <div class="question-content is-large">
        <slot />
      </div>
    </div>
  </div>
</div>
