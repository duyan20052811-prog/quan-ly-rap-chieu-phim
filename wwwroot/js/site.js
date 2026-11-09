(function () {
  var side = document.getElementById('sidebar');
  var btn = document.getElementById('menuBtn');
  if (btn && side) {
    btn.addEventListener('click', function () { side.classList.toggle('open'); });
    document.addEventListener('click', function (e) {
      if (side.classList.contains('open') && !side.contains(e.target) && e.target !== btn) side.classList.remove('open');
    });
  }
})();
